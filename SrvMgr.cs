using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PalServerManager
{
    public class SrvMgr
    {
        #region 字段
        private Process svrProc;
        private Config cfg;
        public Config Config => cfg;
        private Timer monitorTimer;
        private DateTime serverStartTime;
        private bool isShuttingDown = false;
        private bool manualStop = false;
        private bool apiReady = false;
        private readonly HttpClient http;
        private string svrExePath;
        private string workDir;
        public event Action<string> OnLog;
        #endregion

        public string CurrentExePath => svrExePath;

        public SrvMgr(Config c)
        {
            cfg = c;
            http = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{cfg.AdmPwd}"));
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            monitorTimer = new Timer { Interval = 1000 };
            monitorTimer.Tick += MonitorTick;
        }

        #region 路径解析
        private (string exe, string dir) ResolvePaths()
        {
            string exe = cfg.SvrExe;
            string dir = cfg.WorkDir;
            if (!string.IsNullOrEmpty(dir))
            {
                if (!Path.IsPathRooted(exe)) exe = Path.Combine(dir, exe);
                return (exe, dir);
            }
            if (Path.IsPathRooted(exe) && File.Exists(exe))
                return (exe, Path.GetDirectoryName(exe));
            string[] dirs = { AppDomain.CurrentDomain.BaseDirectory, Environment.CurrentDirectory };
            foreach (var d in dirs)
            {
                string full = Path.Combine(d, exe);
                if (File.Exists(full)) return (full, d);
            }
            return (exe, dir);
        }
        #endregion

        #region 启动服务端
        public async void StartSvr()
        {
            if (svrProc != null && !svrProc.HasExited) { Log("服务端已在运行。"); return; }
            Log("启动服务端...");
            var (exe, dir) = ResolvePaths();
            if (!File.Exists(exe)) { Log($"找不到 {exe}。"); return; }
            svrExePath = exe;
            workDir = dir;
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = cfg.SvrArgs,
                WorkingDirectory = dir,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            try
            {
                svrProc = Process.Start(psi);
                if (svrProc == null || svrProc.HasExited) { Log("启动失败。"); return; }
                Log($"已启动 PID:{svrProc.Id} 目录:{dir}");
                manualStop = false;
                isShuttingDown = false;
                apiReady = false;
                if (!monitorTimer.Enabled) monitorTimer.Start();

                Log("等待 REST API 就绪...");
                for (int i = 0; i < 5; i++)
                {
                    if (await CheckApi())
                    {
                        apiReady = true;
                        Log("REST API 已就绪。");
                        break;
                    }
                    await Task.Delay(1000);
                }
                if (!apiReady)
                {
                    Log("警告：REST API 未在 5 秒内就绪，将继续监控。");
                }

                serverStartTime = DateTime.Now;
                Log($"运行时间累计计时已开始。{(cfg.RuntimeSeconds > 0 ? $"将在 {cfg.RuntimeSeconds} 秒后自动重启。" : "自动重启已禁用。")}");
            }
            catch (Exception ex) { Log($"异常: {ex.Message}"); }
        }
        #endregion

        #region 附加进程
        public List<Process> GetProcs()
        {
            return Process.GetProcesses().Where(p => p.ProcessName.Contains("PalServer") && !p.HasExited).ToList();
        }

        public bool AttachProc(Process p)
        {
            if (p == null || p.HasExited) { Log("无效进程。"); return false; }
            if (svrProc != null && !svrProc.HasExited) { Log("已有进程。"); return false; }
            svrProc = p;
            try
            {
                string path = p.MainModule.FileName;
                svrExePath = path;
                workDir = Path.GetDirectoryName(path);
                cfg.SvrExe = path;
                cfg.WorkDir = workDir;
                cfg.Save();
                Log($"已更新配置: {path}");
            }
            catch { var (exe, dir) = ResolvePaths(); svrExePath = exe; workDir = dir; }
            manualStop = false;
            isShuttingDown = false;
            apiReady = true;
            if (!monitorTimer.Enabled) monitorTimer.Start();
            serverStartTime = DateTime.Now;
            Log($"已附加 PID:{p.Id}，运行时间计时已开始。");
            return true;
        }
        #endregion

        #region 监控与运行时间累计重启
        private async void MonitorTick(object s, EventArgs e)
        {
            if (svrProc == null || svrProc.HasExited)
            {
                if (manualStop && !cfg.AutoRestart)
                {
                    Log("手动停止且配置禁止自动重启，监控暂停，服务端保持停止。");
                    monitorTimer.Stop();
                    manualStop = false;
                    return;
                }
                if (svrProc != null)
                {
                    Log("监控发现服务端已退出，正在自动重启...");
                    svrProc.Dispose();
                    svrProc = null;
                }
                StartSvr();
                return;
            }

            if (isShuttingDown || !apiReady) return;

            if (cfg.RuntimeSeconds > 0)
            {
                TimeSpan elapsed = DateTime.Now - serverStartTime;
                if (elapsed.TotalSeconds >= cfg.RuntimeSeconds)
                {
                    Log($"运行时间已累计 {elapsed.TotalSeconds:F0} 秒，达到设定的 {cfg.RuntimeSeconds} 秒，正在关服...");
                    isShuttingDown = true;
                    await ShutdownWithAnnounce();
                }
            }
        }

        private async Task ShutdownWithAnnounce()
        {
            if (svrProc == null || svrProc.HasExited) return;
            int waittime = 60;
            string message = string.IsNullOrEmpty(cfg.ShutdownMessage) ? "服务器即将在60秒后重启" : cfg.ShutdownMessage;
            bool ok = await SendApi("shutdown", $"{{\"waittime\":{waittime},\"message\":\"{message}\"}}");
            if (!ok)
            {
                Log("关服命令发送失败，尝试强制终止。");
                await KillSvrAsync();
                return;
            }
            Log($"关服命令已发送，将在 {waittime} 秒后关闭。");

            int waited = 0;
            int maxWait = (waittime + 10) * 1000;
            while (waited < maxWait && svrProc != null && !svrProc.HasExited)
            {
                await Task.Delay(200);
                waited += 200;
            }
            if (svrProc != null && !svrProc.HasExited)
            {
                Log("关服超时，强制终止。");
                try { svrProc.Kill(); svrProc.WaitForExit(3000); svrProc.Dispose(); } catch { }
                svrProc = null;
            }
            else if (svrProc != null)
            {
                svrProc.Dispose();
                svrProc = null;
            }
            isShuttingDown = false;
            Log("服务端已关闭，监控将自动重启。");
        }
        #endregion

        #region API 命令
        public async Task<bool> SendApi(string ep, string body = null)
        {
            try
            {
                var url = $"{cfg.ApiUrl}/{ep}";
                HttpResponseMessage resp;
                if (string.IsNullOrEmpty(body))
                    resp = await http.GetAsync(url);
                else
                    resp = await http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex) { Log($"API失败: {ex.Message}"); return false; }
        }

        public async Task<string> SendApiGet(string ep)
        {
            try
            {
                var url = $"{cfg.ApiUrl}/{ep}";
                var resp = await http.GetAsync(url);
                if (resp.IsSuccessStatusCode)
                    return await resp.Content.ReadAsStringAsync();
                return null;
            }
            catch (Exception ex) { Log($"API GET 失败: {ex.Message}"); return null; }
        }

        public async Task<bool> CheckApi()
        {
            try
            {
                var url = $"{cfg.ApiUrl}/info";
                var resp = await http.GetAsync(url);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task ShutdownRst()
        {
            if (svrProc == null || svrProc.HasExited) { Log("服务端未运行，直接启动。"); StartSvr(); return; }
            Log("发送关服命令...");
            int waittime = cfg.ShutdownWaittime > 0 ? cfg.ShutdownWaittime : 5;
            string message = string.IsNullOrEmpty(cfg.ShutdownMessage) ? "服务器即将关闭，将自动重启" : cfg.ShutdownMessage;
            bool ok = await SendApi("shutdown", $"{{\"waittime\":{waittime},\"message\":\"{message}\"}}");
            if (!ok)
            {
                Log("关服失败，强制终止。");
                await KillSvrAsync();
                return;
            }
            Log("等待退出...");
            while (svrProc != null && !svrProc.HasExited) await Task.Delay(1000);
            Log("已退出，正在重启...");
            svrProc?.Dispose();
            svrProc = null;
            StartSvr();
        }

        public void UpdateShutdownConfig(int waittime, string message)
        {
            cfg.ShutdownWaittime = waittime;
            cfg.ShutdownMessage = message ?? "";
            cfg.Save();
            Log($"关服配置已更新: 等待 {waittime} 秒, 消息: \"{message}\"");
        }
        #endregion

        #region 强制停止（异步）
        public async Task KillSvrAsync()
        {
            if (svrProc != null && !svrProc.HasExited)
            {
                bool apiSuccess = false;
                try
                {
                    Log("尝试发送关服命令...");
                    var task = SendApi("shutdown", "{\"waittime\":5,\"message\":\"管理员通过控制台强制关闭服务器\"}");
                    apiSuccess = await Task.WhenAny(task, Task.Delay(8000)) == task && task.Result;

                    if (apiSuccess)
                    {
                        int waited = 0;
                        while (waited < 3000 && !svrProc.HasExited)
                        {
                            await Task.Delay(200);
                            waited += 200;
                        }
                        if (svrProc.HasExited)
                            Log("服务端已正常关闭。");
                        else
                            Log("服务端未在预期时间内退出，将强制终止。");
                    }
                    else
                    {
                        Log("关服命令发送超时，强制终止。");
                    }
                }
                catch (Exception ex)
                {
                    Log($"关服尝试失败: {ex.Message}");
                }

                if (svrProc != null && !svrProc.HasExited)
                {
                    try
                    {
                        svrProc.Kill();
                        svrProc.WaitForExit(3000);
                        svrProc.Dispose();
                    }
                    catch { }
                    svrProc = null;
                    Log("已强制停止。");
                }
                else if (svrProc != null)
                {
                    svrProc.Dispose();
                    svrProc = null;
                }
            }

            manualStop = true;
            isShuttingDown = false;
            monitorTimer.Stop();
            Log("监控已停止，服务端保持停止状态。");
        }
        #endregion

        #region 立即终止（同步，用于程序退出）
        public void TerminateNow()
        {
            if (svrProc != null && !svrProc.HasExited)
            {
                try
                {
                    svrProc.Kill();
                    svrProc.WaitForExit(2000);
                    svrProc.Dispose();
                }
                catch { }
                svrProc = null;
            }
            manualStop = true;
            monitorTimer?.Stop();
            Log("已强制终止服务端进程。");
        }
        #endregion

        public bool IsRun => svrProc != null && !svrProc.HasExited;
        public void Log(string msg) => OnLog?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");

        public void ReloadCfg()
        {
            cfg = Config.Reload();
            Log("配置已重载。");
        }

        public void SetRuntime(int sec)
        {
            cfg.RuntimeSeconds = sec;
            cfg.Save();
            Log($"运行时间累计重启已设为 {sec} 秒。");
            if (svrProc != null && !svrProc.HasExited && sec > 0)
            {
                serverStartTime = DateTime.Now;
                Log("计时器已重置。");
            }
        }

        public void SetExe(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Log("无效的文件路径。");
                return;
            }
            cfg.SvrExe = path;
            cfg.WorkDir = Path.GetDirectoryName(path);
            cfg.Save();
            Log($"服务端路径已更新: {path}");
            svrExePath = path;
            workDir = Path.GetDirectoryName(path);
        }

        public string EnsureGameConfig(string exePath)
        {
            if (string.IsNullOrEmpty(exePath)) return null;
            string pwd = GameConfigHelper.EnsureConfig(exePath);
            cfg.AdmPwd = pwd;
            cfg.Save();
            UpdateAuthHeader(pwd);
            Log($"已同步游戏配置：AdminPassword = {pwd}, RESTAPIEnabled=True");
            return pwd;
        }

        private void UpdateAuthHeader(string password)
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"admin:{password}"));
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
        }
    }
}