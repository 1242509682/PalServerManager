using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace PalServerManager
{
    public class Config
    {
        [JsonProperty("管理员密码", Order = 0)]
        public string AdmPwd { get; set; } = "1234";

        [JsonProperty("运行时间累计重启(秒)", Order = 1)]
        public int RuntimeSeconds { get; set; } = 0; // 0 表示禁用

        [JsonProperty("服务端可执行文件", Order = 2)]
        public string SvrExe { get; set; } = "PalServer.exe";

        [JsonProperty("服务端启动参数", Order = 3)]
        public string SvrArgs { get; set; } = "-port=8211 -log -NoTransferFromFiltering -BatchMode -nographics -ForceRespawnDinosOnEmptySpawner -OptimizeGC -UseLessRAM -NoHangDetection -EnableCommunityMode";

        [JsonProperty("API 基础地址", Order = 4)]
        public string ApiUrl { get; set; } = "http://127.0.0.1:8212/v1/api";

        [JsonProperty("工作目录", Order = 5)]
        public string WorkDir { get; set; } = "";

        [JsonProperty("UE4SS GitHub 直链", Order = 6)]
        public string Ue4Url { get; set; } = "https://github.com/Okaetsu/RE-UE4SS/releases/download/experimental-palworld/UE4SS-Palworld.zip";

        [JsonProperty("PalSchema GitHub 直链", Order = 7)]
        public string PsUrl { get; set; } = "https://github.com/Okaetsu/PalSchema/releases/download/0.6.0/PalSchema_0.6.0.zip";

        [JsonProperty("强制停止后自动重启", Order = 8)]
        public bool AutoRestart { get; set; } = true;

        // 新增 BanEntry 类
        public class BanEntry
        {
            [JsonProperty("userId")]
            public string UserId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("ip")]
            public string IP { get; set; }

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("banTime")]
            public DateTime BanTime { get; set; } = DateTime.Now;
        }

        // 替换原 BanList
        [JsonProperty("封禁玩家列表", Order = 9)]
        public List<BanEntry> BanList { get; set; } = new List<BanEntry>();

        [JsonProperty("界面刷新间隔(毫秒)", Order = 10)]
        public int UiRefreshInterval { get; set; } = 500;

        [JsonProperty("启动窗口进程列表刷新间隔(毫秒)", Order = 12)]
        public int LaunchRefreshInterval { get; set; } = 2000;

        [JsonProperty("自动最小化主窗口", Order = 13)]
        public bool AutoMinimize { get; set; } = false;

        [JsonProperty("关服等待时间(秒)", Order = 14)]
        public int ShutdownWaittime { get; set; } = 5;

        [JsonProperty("关服提示消息", Order = 15)]
        public string ShutdownMessage { get; set; } = "服务器即将关闭，将自动重启";

        [JsonProperty("广播消息列表", Order = 16)]
        public List<string> BroadcastMessages { get; set; } = new();

        [JsonProperty("广播无限循环", Order = 17)]
        public bool BroadcastLoopInfinite { get; set; } = false;

        [JsonProperty("广播循环间隔(秒)", Order = 18)]
        public int BroadcastInterval { get; set; } = 60;

        private static string CfgPath = "config.json";

        public static Config Load()
        {
            if (File.Exists(CfgPath))
            {
                try { return JsonConvert.DeserializeObject<Config>(File.ReadAllText(CfgPath)) ?? new Config(); }
                catch { return new Config(); }
            }
            var def = new Config();
            File.WriteAllText(CfgPath, JsonConvert.SerializeObject(def, Formatting.Indented));
            return def;
        }

        public void Save() => File.WriteAllText(CfgPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        public static Config Reload() => Load();
    }
}