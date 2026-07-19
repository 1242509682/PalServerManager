using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PalServerManager
{
    public static class GameConfigHelper
    {
        // 这些键必须加引号（字符串）
        public static readonly HashSet<string> AlwaysQuoteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AdminPassword",
            "ServerName",
            "ServerDescription",
            "ServerPassword",
            "PublicIP",
            "Region",
            "BanListURL",
            "AdditionalDropItemWhenPlayerKillingInPvPMode",
            "DenyTechnologyList",
            "RandomizerSeed"
        };

        // 这些键是数字或布尔，不加引号
        public static readonly HashSet<string> NeverQuoteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Difficulty", "RandomizerType", "DeathPenalty",  // 枚举值，不加引号
            "bIsRandomizerPalLevelRandom",
            "DayTimeSpeedRate", "NightTimeSpeedRate", "ExpRate", "PalCaptureRate",
            "PalSpawnNumRate", "PalDamageRateAttack", "PalDamageRateDefense",
            "PlayerDamageRateAttack", "PlayerDamageRateDefense",
            "PlayerStomachDecreaceRate", "PlayerStaminaDecreaceRate",
            "PlayerAutoHPRegeneRate", "PlayerAutoHpRegeneRateInSleep",
            "PalStomachDecreaceRate", "PalStaminaDecreaceRate",
            "PalAutoHPRegeneRate", "PalAutoHpRegeneRateInSleep",
            "BuildObjectHpRate", "BuildObjectDamageRate", "BuildObjectDeteriorationDamageRate",
            "CollectionDropRate", "CollectionObjectHpRate", "CollectionObjectRespawnSpeedRate",
            "EnemyDropItemRate",
            "bEnablePlayerToPlayerDamage", "bEnableFriendlyFire", "bEnableInvaderEnemy",
            "bActiveUNKO", "bEnableAimAssistPad", "bEnableAimAssistKeyboard",
            "DropItemMaxNum", "PhysicsActiveDropItemMaxNum", "DropItemMaxNum_UNKO",
            "BaseCampMaxNum", "BaseCampWorkerMaxNum", "DropItemAliveMaxHours",
            "bAutoResetGuildNoOnlinePlayers", "AutoResetGuildTimeNoOnlinePlayers",
            "GuildPlayerMaxNum", "BaseCampMaxNumInGuild",
            "PalEggDefaultHatchingTime", "WorkSpeedRate", "AutoSaveSpan",
            "bIsMultiplay", "bIsPvP", "bHardcore", "bPalLost",
            "bCharacterRecreateInHardcore", "bCanPickupOtherGuildDeathPenaltyDrop",
            "bEnableNonLoginPenalty", "bEnableFastTravel", "bEnableFastTravelOnlyBaseCamp",
            "bIsStartLocationSelectByMap", "bExistPlayerAfterLogout",
            "bEnableDefenseOtherGuildPlayer", "bInvisibleOtherGuildBaseCampAreaFX",
            "bBuildAreaLimit", "ItemWeightRate", "CoopPlayerMaxNum", "ServerPlayerMaxNum",
            "bAllowClientMod", "PublicPort", "RCONEnabled", "RCONPort", "bUseAuth",
            "ChatPostLimitPerMinute", "RESTAPIEnabled", "RESTAPIPort",
            "bShowPlayerList", "bIsUseBackupSaveData", "LogFormatType",
            "bIsShowJoinLeftMessage", "SupplyDropSpan", "EnablePredatorBossPal",
            "MaxBuildingLimitNum", "ServerReplicatePawnCullDistance",
            "bAllowGlobalPalboxExport", "bAllowGlobalPalboxImport",
            "EquipmentDurabilityDamageRate", "ItemContainerForceMarkDirtyInterval",
            "PlayerDataPalStorageUpdateCheckTickInterval", "ItemCorruptionMultiplier",
            "MonsterFarmActionSpeedRate", "GuildRejoinCooldownMinutes",
            "AutoTransferMasterCheckIntervalSeconds", "AutoTransferMasterThresholdDays",
            "MaxGuildsPerFrame", "BlockRespawnTime", "RespawnPenaltyDurationThreshold",
            "RespawnPenaltyTimeScale", "bDisplayPvPItemNumOnWorldMap_BaseCamp",
            "bDisplayPvPItemNumOnWorldMap_Player",
            "AdditionalDropItemNumWhenPlayerKillingInPvPMode",
            "bAdditionalDropItemWhenPlayerKillingInPvPMode",
            "bEnableVoiceChat", "VoiceChatMaxVolumeDistance", "VoiceChatZeroVolumeDistance",
            "bAllowEnhanceStat_Health", "bAllowEnhanceStat_Attack",
            "bAllowEnhanceStat_Stamina", "bAllowEnhanceStat_Weight",
            "bAllowEnhanceStat_WorkSpeed", "bEnableBuildingPlayerUIdDisplay",
            "BuildingNameDisplayCacheTTLSeconds",
            "CrossplayPlatforms" // 括号表达式，不加引号
        };

        public static string EnsureConfig(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                throw new ArgumentException("无效的服务端 exe 路径");

            string win64Dir = Path.GetDirectoryName(exePath);
            string binariesDir = Path.GetDirectoryName(win64Dir);
            string palRoot = Path.GetDirectoryName(binariesDir);
            string configFile = Path.Combine(palRoot, "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");

            if (!File.Exists(configFile))
                throw new FileNotFoundException($"未找到配置文件：{configFile}");

            string content = File.ReadAllText(configFile);
            string pattern = @"OptionSettings\s*=\s*\(([^)]*)\)";
            var match = Regex.Match(content, pattern);
            if (!match.Success)
                throw new Exception("未找到 OptionSettings 行");

            string optionLine = match.Groups[1].Value;
            var dict = ParseOptionSettings(optionLine);

            // 确保 AdminPassword 存在且不为空
            if (!dict.ContainsKey("AdminPassword") || string.IsNullOrEmpty(dict["AdminPassword"]))
                dict["AdminPassword"] = "1234";

            // 确保 RESTAPIEnabled=True
            dict["RESTAPIEnabled"] = "True";

            // 重新构建 OptionSettings 行
            var pairList = new List<string>();
            foreach (var kv in dict)
            {
                string key = kv.Key;
                string val = kv.Value;
                bool needQuote = false;

                // 1. 强制加引号的键
                if (AlwaysQuoteKeys.Contains(key))
                {
                    needQuote = true;
                }
                // 2. 强制不加引号的键（数字/布尔/枚举/括号）
                else if (NeverQuoteKeys.Contains(key))
                {
                    needQuote = false;
                }
                // 3. 括号表达式
                else if (val.StartsWith("(") && val.EndsWith(")"))
                {
                    needQuote = false;
                }
                // 4. 纯数字或布尔
                else if (Regex.IsMatch(val, @"^-?\d+(\.\d+)?$") ||
                         val.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                         val.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    needQuote = false;
                }
                // 5. 其他情况（如中文、URL、未知字符串）都加引号
                else
                {
                    needQuote = true;
                }

                // 如果值是空字符串，统一改为 ""
                if (string.IsNullOrEmpty(val))
                {
                    pairList.Add($"{key}=\"\"");
                    continue;
                }

                // 如果已经带引号，不再重复加
                if (val.StartsWith("\"") && val.EndsWith("\""))
                {
                    pairList.Add($"{key}={val}");
                    continue;
                }

                if (needQuote)
                    val = $"\"{val}\"";

                pairList.Add($"{key}={val}");
            }

            string newOptionLine = string.Join(",", pairList);
            string newContent = Regex.Replace(content, pattern, $"OptionSettings=({newOptionLine})");
            File.WriteAllText(configFile, newContent);

            return dict["AdminPassword"];
        }

        public static string ReadAdminPassword(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return null;

            string win64Dir = Path.GetDirectoryName(exePath);
            string binariesDir = Path.GetDirectoryName(win64Dir);
            string palRoot = Path.GetDirectoryName(binariesDir);
            string configFile = Path.Combine(palRoot, "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");

            if (!File.Exists(configFile))
                return null;

            string content = File.ReadAllText(configFile);
            var match = Regex.Match(content, @"OptionSettings\s*=\s*\(([^)]*)\)");
            if (!match.Success)
                return null;

            string optionLine = match.Groups[1].Value;
            var dict = ParseOptionSettings(optionLine);
            return dict.ContainsKey("AdminPassword") ? dict["AdminPassword"] : null;
        }

        private static Dictionary<string, string> ParseOptionSettings(string line)
        {
            var dict = new Dictionary<string, string>();
            int i = 0, len = line.Length;

            while (i < len)
            {
                while (i < len && char.IsWhiteSpace(line[i])) i++;
                if (i >= len) break;

                int keyStart = i;
                while (i < len && line[i] != '=') i++;
                if (i >= len) break;
                string key = line.Substring(keyStart, i - keyStart).Trim();
                i++;

                while (i < len && char.IsWhiteSpace(line[i])) i++;

                string value = ReadValue(line, ref i);
                if (!dict.ContainsKey(key))
                    dict[key] = value;

                while (i < len && line[i] != ',') i++;
                if (i < len && line[i] == ',') i++;
            }
            return dict;
        }

        private static string ReadValue(string line, ref int pos)
        {
            int len = line.Length;
            if (pos >= len) return "";

            char first = line[pos];
            if (first == '"')
            {
                pos++;
                int start = pos;
                while (pos < len)
                {
                    if (line[pos] == '"' && (pos == 0 || line[pos - 1] != '\\'))
                        break;
                    pos++;
                }
                string val = line.Substring(start, pos - start);
                pos++;
                return val;
            }
            else if (first == '(')
            {
                int depth = 0;
                int start = pos;
                while (pos < len)
                {
                    if (line[pos] == '(') depth++;
                    else if (line[pos] == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            pos++;
                            break;
                        }
                    }
                    pos++;
                }
                return line.Substring(start, pos - start);
            }
            else
            {
                int start = pos;
                while (pos < len && line[pos] != ',')
                {
                    if (line[pos] == '(')
                    {
                        ReadValue(line, ref pos);
                        continue;
                    }
                    pos++;
                }
                return line.Substring(start, pos - start).TrimEnd();
            }
        }
    }
}