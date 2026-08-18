using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PalServerManager;

public class Ue4Mgr
{
    #region 字段
    public string WinDir { get; private set; } // 改为公共属性
    private HttpClient http;
    private Config cfg;
    public event Action<string> OnLog;
    public bool IsReady { get; private set; } = false;
    #endregion

    public Ue4Mgr(Config c)
    {
        cfg = c;
        http = new HttpClient();
        http.Timeout = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// 根据服务端 EXE 路径推导正确的 UE4SS 安装目标目录：根目录\Pal\Binaries\Win64
    /// </summary>
    private string GetTargetWinDirFromExe(string exePath)
    {
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            return null;

        string rootDir = Path.GetDirectoryName(exePath); // PalServer.exe 所在目录
        return Path.Combine(rootDir, "Pal", "Binaries", "Win64");
    }

    /// <summary>
    /// 手动设置服务端 EXE 路径，并自动推导正确的目标目录
    /// </summary>
    public bool SetWinDirFromExe(string exePath)
    {
        string target = GetTargetWinDirFromExe(exePath);
        if (string.IsNullOrEmpty(target))
        {
            IsReady = false;
            return false;
        }

        WinDir = target;
        IsReady = true;
        Log($"UE4SS 目标目录已设置为: {WinDir}");
        if (!File.Exists(Path.Combine(WinDir, "dwmapi.dll")))
            Log("当前目标目录未找到 dwmapi.dll，可能需要安装 UE4SS。");
        else
            Log("已在目标目录找到 dwmapi.dll。");
        return true;
    }

    /// <summary>
    /// 自动搜索：尝试在常见位置查找 dwmapi.dll，找到则设置 WinDir
    /// </summary>
    public bool AutoDetect()
    {
        if (IsReady && !string.IsNullOrEmpty(WinDir) && File.Exists(Path.Combine(WinDir, "dwmapi.dll")))
            return true;

        Log("开始自动搜索 UE4SS 安装位置...");

        // 候选目录：程序目录、服务端根目录、以及可能的 Pal\Binaries\Win64
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string exeRoot = null;
        if (!string.IsNullOrEmpty(cfg.SvrExe) && File.Exists(cfg.SvrExe))
            exeRoot = Path.GetDirectoryName(cfg.SvrExe);

        var candidates = new System.Collections.Generic.List<string> { baseDir };
        if (!string.IsNullOrEmpty(exeRoot))
        {
            candidates.Add(exeRoot);
            candidates.Add(Path.Combine(exeRoot, "Pal", "Binaries", "Win64"));
            candidates.Add(Path.Combine(exeRoot, "Binaries", "Win64")); // 防止旧错误路径
        }

        foreach (string dir in candidates.Distinct())
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                continue;

            Log($"搜索目录: {dir}");
            string dllPath = Path.Combine(dir, "dwmapi.dll");
            if (File.Exists(dllPath))
            {
                WinDir = dir;
                IsReady = true;
                Log($"自动定位到 UE4SS 目录: {WinDir}");
                return true;
            }

            // 若当前目录不是 Pal\Binaries\Win64，尝试在子目录中递归搜索（限制深度）
            if (!dir.EndsWith("Win64"))
            {
                string found = SearchFileRecursive(dir, "dwmapi.dll", 3);
                if (!string.IsNullOrEmpty(found))
                {
                    WinDir = Path.GetDirectoryName(found);
                    IsReady = true;
                    Log($"自动定位到 UE4SS 目录: {WinDir}");
                    return true;
                }
            }
        }

        Log("未能在任何候选目录中找到 dwmapi.dll。");
        return false;
    }

    private string SearchFileRecursive(string dir, string fileName, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth > maxDepth || !Directory.Exists(dir))
            return null;

        try
        {
            string filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath))
                return filePath;

            foreach (string subDir in Directory.GetDirectories(dir))
            {
                string result = SearchFileRecursive(subDir, fileName, maxDepth, currentDepth + 1);
                if (result != null)
                    return result;
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (PathTooLongException) { }
        return null;
    }

    private void Log(string msg) => OnLog?.Invoke($"[UE4SS] {msg}");

    /// <summary>
    /// 判断 UE4SS 是否已安装（即 WinDir 下存在 dwmapi.dll）
    /// </summary>
    public bool IsInstalled()
    {
        if (!IsReady) return false;
        return File.Exists(Path.Combine(WinDir, "dwmapi.dll"));
    }

    // ---------- 安装与卸载 ----------
    public async Task<bool> Install()
    {
        if (!IsReady) { Log("UE4SS 不可用，请先选择有效的服务端路径。"); return false; }
        if (IsInstalled()) { Log("UE4SS 已安装。"); return true; }

        Log("开始安装 UE4SS...");
        string ue4Zip = await DownloadFile(cfg.Ue4Url);
        if (string.IsNullOrEmpty(ue4Zip)) { Log("UE4SS 下载失败。"); return false; }

        string ue4Extract = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            ZipFile.ExtractToDirectory(ue4Zip, ue4Extract);

            string ue4Src = ue4Extract;
            var dirs = Directory.GetDirectories(ue4Extract);
            if (dirs.Length == 1 && Directory.GetFiles(ue4Extract).Length == 0)
                ue4Src = dirs[0];

            foreach (var item in Directory.GetFileSystemEntries(ue4Src))
            {
                string name = Path.GetFileName(item);
                if (name.Equals("dwmapi.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                string dest = Path.Combine(WinDir, name);
                if (Directory.Exists(item))
                    CopyDir(item, dest);
                else
                    File.Copy(item, dest, true);
            }

            string srcDll = Path.Combine(ue4Src, "dwmapi.dll");
            if (File.Exists(srcDll))
                File.Copy(srcDll, Path.Combine(WinDir, "dwmapi.dll"), true);
            else
                Log("警告：未找到 dwmapi.dll。");

            string srcMods = Path.Combine(ue4Src, "Mods");
            string targetModsRoot = Path.Combine(WinDir, "ue4ss", "Mods");
            if (Directory.Exists(srcMods))
                CopyDir(srcMods, targetModsRoot);
            else
                Directory.CreateDirectory(targetModsRoot);

            Log("UE4SS 核心安装完成。");
        }
        finally
        {
            try { File.Delete(ue4Zip); Directory.Delete(ue4Extract, true); } catch { }
        }

        Log("安装 PalSchema...");
        string psZip = await DownloadFile(cfg.PsUrl);
        if (string.IsNullOrEmpty(psZip))
        {
            Log("PalSchema 下载失败，但 UE4SS 已安装。");
        }
        else
        {
            string psExtract = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                ZipFile.ExtractToDirectory(psZip, psExtract);
                string psSrc = psExtract;
                var psDirs = Directory.GetDirectories(psExtract);
                if (psDirs.Length == 1 && Directory.GetFiles(psExtract).Length == 0)
                    psSrc = psDirs[0];

                string psTarget = Path.Combine(WinDir, "ue4ss", "Mods", "PalSchema");
                if (Directory.Exists(psTarget)) Directory.Delete(psTarget, true);
                Directory.CreateDirectory(psTarget);
                CopyDir(psSrc, psTarget);
                Log("PalSchema 安装完成。");
            }
            finally
            {
                try { File.Delete(psZip); Directory.Delete(psExtract, true); } catch { }
            }
        }

        string modsDir = Path.Combine(WinDir, "ue4ss", "Mods");
        string modsTxt = Path.Combine(modsDir, "mods.txt");
        Directory.CreateDirectory(modsDir);
        File.WriteAllText(modsTxt, "# UE4SS Mods\nPalSchema : 1\n");
        Log("mods.txt 已更新。");

        Log("安装完成。");
        return true;
    }

    public void Uninstall()
    {
        if (!IsReady) { Log("UE4SS 不可用。"); return; }
        if (!IsInstalled()) { Log("UE4SS 未安装。"); return; }
        Log($"开始卸载 UE4SS（目录：{WinDir}）...");

        string dllPath = Path.Combine(WinDir, "dwmapi.dll");
        string ue4Dir = Path.Combine(WinDir, "ue4ss");
        bool anyError = false;

        bool DeleteWithRetry(string path, bool isDirectory)
        {
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    if (isDirectory)
                    {
                        if (!Directory.Exists(path)) return true;
                        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                            try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                        Directory.Delete(path, true);
                        Log($"成功删除目录：{path}");
                        return true;
                    }
                    else
                    {
                        if (!File.Exists(path)) return true;
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                        Log($"成功删除文件：{path}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log($"第 {attempt} 次删除失败 ({path})：{ex.Message}");
                    System.Threading.Thread.Sleep(attempt * 300);
                }
            }

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "UE4SS_Uninstall_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                if (isDirectory)
                {
                    string tempMove = Path.Combine(Path.GetTempPath(), "UE4SS_Dir_" + Guid.NewGuid().ToString());
                    Directory.Move(path, tempMove);
                    Log($"目录已移到临时：{tempMove}，正在删除...");
                    Directory.Delete(tempMove, true);
                    return true;
                }
                else
                {
                    string tempFile = Path.Combine(tempDir, Path.GetFileName(path));
                    File.Move(path, tempFile);
                    File.Delete(tempFile);
                    Log($"文件已移到临时并删除：{tempFile}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"移动删除也失败：{ex.Message}");
                return false;
            }
        }

        if (File.Exists(dllPath))
        {
            if (!DeleteWithRetry(dllPath, false))
            {
                Log($"无法删除 dwmapi.dll，请手动删除：{dllPath}");
                anyError = true;
            }
        }

        if (Directory.Exists(ue4Dir))
        {
            if (!DeleteWithRetry(ue4Dir, true))
            {
                Log($"无法删除 ue4ss 目录，请手动删除：{ue4Dir}");
                anyError = true;
            }
        }

        if (anyError)
        {
            MessageBox.Show(
                $"部分文件未能自动删除，请手动删除以下路径：\n{dllPath}\n{ue4Dir}",
                "卸载不完整",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
        else
        {
            Log("卸载完成，所有文件已清理。");
        }
    }

    private async Task<string> DownloadFile(string url)
    {
        string zipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
        try
        {
            Log($"下载 {url} ...");
            var resp = await http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            using (var fs = new FileStream(zipPath, FileMode.Create))
                await resp.Content.CopyToAsync(fs);
            return zipPath;
        }
        catch (Exception ex)
        {
            Log($"下载失败: {ex.Message}");
            Log("提示: 若网络连接困难，请使用代理或手动下载。");
            try { File.Delete(zipPath); } catch { }
            return null;
        }
    }

    private void CopyDir(string src, string dst)
    {
        if (!Directory.Exists(dst)) Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src))
        {
            string dest = Path.Combine(dst, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }
        foreach (var dir in Directory.GetDirectories(src))
        {
            string dest = Path.Combine(dst, Path.GetFileName(dir));
            CopyDir(dir, dest);
        }
    }
}