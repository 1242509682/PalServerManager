using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PalServerManager
{
    public class ModManager
    {
        private Config cfg;
        private Ue4Mgr ue4;
        public string MyModFolder { get; }

        // 预设安装路径（相对Pal根目录，即PalServer.exe所在目录）
        private static readonly Dictionary<string, string> PresetPaths = new()
        {
            ["UE4SS"] = @"Pal\Binaries\Win64\ue4ss\Mods",
            ["PalSchema"] = @"Pal\Binaries\Win64\ue4ss\Mods\PalSchema\mods",
            ["Pak(~mods)"] = @"Pal\Content\Paks\~mods",
            ["Pak(LogicMods)"] = @"Pal\Content\Paks\LogicMods",
            ["Pak(~WorkshopMods)"] = @"Pal\Content\Paks\~WorkshopMods",
            ["服务器根目录"] = "",   // 空字符串表示直接复制到根目录
        };

        public ModManager(Config config, Ue4Mgr ue4Manager)
        {
            cfg = config;
            ue4 = ue4Manager;
            MyModFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyMod");
            if (!Directory.Exists(MyModFolder))
                Directory.CreateDirectory(MyModFolder);
        }

        // 获取所有Mod文件夹名
        public List<string> GetModFolders()
        {
            if (!Directory.Exists(MyModFolder)) return new List<string>();
            return Directory.GetDirectories(MyModFolder)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }

        // 获取Mod下所有文件（相对路径）
        public List<string> GetModFiles(string modName)
        {
            string modPath = Path.Combine(MyModFolder, modName);
            if (!Directory.Exists(modPath)) return new List<string>();
            return Directory.GetFiles(modPath, "*", SearchOption.AllDirectories)
                .Select(f => GetRelativePath(modPath, f))
                .ToList();
        }

        // 兼容 .NET Framework 的相对路径计算
        private string GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath)) return fullPath;
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basePath += Path.DirectorySeparatorChar;
            var uri = new Uri(basePath);
            var target = new Uri(fullPath);
            return Uri.UnescapeDataString(uri.MakeRelativeUri(target).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        // 检查Mod是否已安装（至少有一个文件映射）
        public bool IsInstalled(string modName)
        {
            return cfg.ModFileInstallMap.ContainsKey(modName) && cfg.ModFileInstallMap[modName].Count > 0;
        }

        // 获取已安装的文件映射（相对路径 -> 完整目标路径）
        public Dictionary<string, string> GetInstallMap(string modName)
        {
            if (cfg.ModFileInstallMap.TryGetValue(modName, out var map))
                return map;
            return new Dictionary<string, string>();
        }

        // 获取Pal根目录（即PalServer.exe所在目录）
        public string GetPalRoot()
        {
            if (string.IsNullOrEmpty(cfg.SvrExe) || !File.Exists(cfg.SvrExe))
                return null;
            // 直接返回 exe 所在目录，即根目录
            return Path.GetDirectoryName(cfg.SvrExe);
        }

        // 安装Mod（保存映射）
        public bool InstallMod(string modName, Dictionary<string, string> fileMap)
        {
            if (fileMap == null || fileMap.Count == 0)
            {
                MessageBox.Show("没有文件需要安装。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string palRoot = GetPalRoot();
            if (string.IsNullOrEmpty(palRoot))
            {
                MessageBox.Show("未找到服务端安装目录，请先通过“启动服务”指定服务端路径。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // 检查UE4SS和PalSchema依赖（若目标包含这些路径则检查）
            bool needUE4SS = fileMap.Values.Any(path => path.Contains("ue4ss\\Mods") && !path.Contains("PalSchema"));
            bool needPalSchema = fileMap.Values.Any(path => path.Contains("PalSchema"));
            if ((needUE4SS || needPalSchema) && (ue4 == null || !ue4.IsReady || !ue4.IsInstalled()))
            {
                MessageBox.Show("需要安装 UE4SS（包含 PalSchema）才能安装此Mod。\n请先通过“UE4SS”按钮安装。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string modSource = Path.Combine(MyModFolder, modName);
            var newMap = new Dictionary<string, string>();

            try
            {
                foreach (var kv in fileMap)
                {
                    string relativePath = kv.Key;
                    string targetFullPath = kv.Value;
                    if (string.IsNullOrEmpty(targetFullPath))
                    {
                        MessageBox.Show($"文件 {relativePath} 的目标路径为空，请补全。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    string targetDir = Path.GetDirectoryName(targetFullPath);
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    string sourceFile = Path.Combine(modSource, relativePath);
                    if (!File.Exists(sourceFile))
                    {
                        MessageBox.Show($"源文件不存在：{sourceFile}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    File.Copy(sourceFile, targetFullPath, true);
                    newMap[relativePath] = targetFullPath;
                }

                // 保存映射
                cfg.ModFileInstallMap[modName] = newMap;
                cfg.Save();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // 卸载Mod（删除所有已安装文件，并移除映射）
        public bool UninstallMod(string modName)
        {
            if (!cfg.ModFileInstallMap.TryGetValue(modName, out var map))
            {
                MessageBox.Show("该Mod未安装。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            bool anyError = false;
            foreach (var kv in map)
            {
                try
                {
                    if (File.Exists(kv.Value))
                        File.Delete(kv.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除文件失败：{kv.Value}\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    anyError = true;
                }
            }

            // 清理空目录（从最深开始）
            if (!anyError)
            {
                var dirs = map.Values.Select(Path.GetDirectoryName).Distinct();
                foreach (var dir in dirs.OrderByDescending(d => d.Length))
                {
                    try
                    {
                        if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                            Directory.Delete(dir);
                    }
                    catch { }
                }
            }

            // 移除映射
            cfg.ModFileInstallMap.Remove(modName);
            cfg.Save();

            if (anyError)
                MessageBox.Show("卸载完成，但部分文件删除失败，请检查。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show("卸载成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return !anyError;
        }

        // 打开目录（已安装打开安装目录，否则打开MyMod源目录）
        public void OpenModFolder(string modName)
        {
            string path;
            if (cfg.ModFileInstallMap.TryGetValue(modName, out var map) && map.Count > 0)
            {
                string firstFile = map.Values.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstFile))
                    path = Path.GetDirectoryName(firstFile);
                else
                    path = Path.Combine(MyModFolder, modName);
            }
            else
            {
                path = Path.Combine(MyModFolder, modName);
            }

            if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
            else
                MessageBox.Show("目录不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // 新增重载，支持模组名（自动包含模组文件夹）
        public string GetPresetFullPath(string presetKey, string modName, string relativePath)
        {
            string palRoot = GetPalRoot();
            if (string.IsNullOrEmpty(palRoot)) return null;
            if (!PresetPaths.TryGetValue(presetKey, out string relativeBase))
                return null;
            if (string.IsNullOrEmpty(relativeBase))
                return Path.Combine(palRoot, relativePath);
            string fullBase = Path.Combine(palRoot, relativeBase);
            // 加入模组文件夹名作为子目录
            return Path.Combine(fullBase, modName, relativePath);
        }

        // 获取所有预设键名
        public List<string> GetPresetKeys() => PresetPaths.Keys.ToList();
    }
}