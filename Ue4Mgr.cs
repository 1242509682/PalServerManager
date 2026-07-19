using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PalServerManager;

public class Ue4Mgr
{
    #region 字段
    private string WinDir;
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

    public bool SetWinDirFromExe(string exePath)
    {
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            IsReady = false;
            return false;
        }
        WinDir = Path.GetDirectoryName(exePath);
        IsReady = true;
        Log($"UE4SS 工作目录已设置为: {WinDir}");
        return true;
    }

    private void Log(string msg) => OnLog?.Invoke($"[UE4SS] {msg}");

    public bool IsInstalled()
    {
        if (!IsReady) return false;
        return File.Exists(Path.Combine(WinDir, "dwmapi.dll"));
    }

    public async Task<bool> Install()
    {
        if (!IsReady) { Log("UE4SS 不可用，请先附加或选择服务端 exe。"); return false; }
        if (IsInstalled()) { Log("UE4SS 已安装。"); return true; }

        Log("开始安装 UE4SS...");
        string ue4Zip = await DownloadFile(cfg.Ue4Url);
        if (string.IsNullOrEmpty(ue4Zip)) { Log("UE4SS 下载失败。"); return false; }

        string ue4Extract = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            ZipFile.ExtractToDirectory(ue4Zip, ue4Extract);

            // 确定源目录（可能包含单层子目录）
            string ue4Src = ue4Extract;
            var dirs = Directory.GetDirectories(ue4Extract);
            if (dirs.Length == 1 && Directory.GetFiles(ue4Extract).Length == 0)
                ue4Src = dirs[0];

            // 1. 复制除 dwmapi.dll 外的所有文件到 Win64 根目录
            foreach (var item in Directory.GetFileSystemEntries(ue4Src))
            {
                string name = Path.GetFileName(item);
                if (name.Equals("dwmapi.dll", StringComparison.OrdinalIgnoreCase))
                    continue; // 单独处理

                string dest = Path.Combine(WinDir, name);
                if (Directory.Exists(item))
                    CopyDir(item, dest);
                else
                    File.Copy(item, dest, true);
            }

            // 2. 复制 dwmapi.dll 到 Win64 根目录
            string srcDll = Path.Combine(ue4Src, "dwmapi.dll");
            if (File.Exists(srcDll))
                File.Copy(srcDll, Path.Combine(WinDir, "dwmapi.dll"), true);
            else
                Log("警告：未找到 dwmapi.dll。");

            // 3. 处理 Mods 目录：如果源目录中有 Mods 文件夹，将其内容移到 Win64/ue4ss/Mods/
            string srcMods = Path.Combine(ue4Src, "Mods");
            string targetModsRoot = Path.Combine(WinDir, "ue4ss", "Mods");
            if (Directory.Exists(srcMods))
            {
                // 复制 Mods 下的所有内容到 targetModsRoot
                CopyDir(srcMods, targetModsRoot);
            }
            else
            {
                // 如果源没有 Mods，则创建空的 ue4ss/Mods
                Directory.CreateDirectory(targetModsRoot);
            }

            Log("UE4SS 核心安装完成。");
        }
        finally
        {
            try { File.Delete(ue4Zip); Directory.Delete(ue4Extract, true); } catch { }
        }

        // 安装 PalSchema（单独下载）
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

        // 确保 mods.txt 存在且内容正确
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
        Log("卸载 UE4SS...");
        string dll = Path.Combine(WinDir, "dwmapi.dll");
        if (File.Exists(dll))
        {
            try { File.Delete(dll); Log("删除 dwmapi.dll"); }
            catch (Exception ex) { Log($"删除 dwmapi.dll 失败: {ex.Message}"); }
        }
        string ue4Dir = Path.Combine(WinDir, "ue4ss");
        if (Directory.Exists(ue4Dir))
        {
            try { Directory.Delete(ue4Dir, true); Log("删除 ue4ss 目录"); }
            catch (Exception ex) { Log($"删除 ue4ss 目录失败: {ex.Message}"); }
        }
        Log("卸载完成。");
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
            Log("提示: 若网络连接 GitHub 困难，请安装 Watt Toolkit 并开启 GitHub 加速。");
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