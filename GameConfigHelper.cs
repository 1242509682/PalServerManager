using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

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
            "RandomizerSeed"
        };

        // 这些键是数字或布尔，不加引号
        public static readonly HashSet<string> NeverQuoteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Difficulty", "RandomizerType", "DeathPenalty",
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
            "CrossplayPlatforms",
            "DenyTechnologyList" // 手动添加，保证不加引号
        };

        /// <summary>
        /// 向上搜索 PalWorldSettings.ini，返回完整路径或 null
        /// </summary>
        public static string FindConfigFile(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return null;

            string currentDir = Path.GetDirectoryName(exePath);
            int maxLevels = 10;
            while (maxLevels-- > 0 && currentDir != null)
            {
                // 尝试常见路径
                string candidate1 = Path.Combine(currentDir, "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");
                if (File.Exists(candidate1)) return candidate1;

                string candidate2 = Path.Combine(currentDir, "Pal", "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");
                if (File.Exists(candidate2)) return candidate2;

                // 尝试父目录
                string parent = Directory.GetParent(currentDir)?.FullName;
                if (parent != null)
                {
                    string candidate3 = Path.Combine(parent, "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");
                    if (File.Exists(candidate3)) return candidate3;
                    string candidate4 = Path.Combine(parent, "Pal", "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");
                    if (File.Exists(candidate4)) return candidate4;
                }

                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            return null;
        }

        /// <summary>
        /// 检查配置文件中是否包含必需的键（AdminPassword, RESTAPIEnabled）
        /// </summary>
        public static (bool valid, List<string> missingKeys) CheckRequiredKeys(string configPath)
        {
            if (!File.Exists(configPath))
                return (false, new List<string> { "文件不存在" });

            string content = File.ReadAllText(configPath);
            var match = Regex.Match(content, @"OptionSettings\s*=\s*\(([^)]*)\)");
            if (!match.Success)
                return (false, new List<string> { "OptionSettings 行缺失" });

            var dict = ParseOptionSettings(match.Groups[1].Value);
            var required = new HashSet<string> { "AdminPassword", "RESTAPIEnabled" };
            var missing = required.Where(k => !dict.ContainsKey(k) || string.IsNullOrEmpty(dict[k])).ToList();
            return (missing.Count == 0, missing);
        }

        /// <summary>
        /// 确保配置文件完整（补全 AdminPassword 和 RESTAPIEnabled），传入完整文件路径
        /// </summary>
        public static string EnsureConfig(string configFile)
        {
            if (string.IsNullOrEmpty(configFile) || !File.Exists(configFile))
                throw new FileNotFoundException("配置文件不存在", configFile);

            string content = File.ReadAllText(configFile);
            string pattern = @"OptionSettings\s*=\s*\(([^)]*)\)";
            var match = Regex.Match(content, pattern);
            if (!match.Success)
                throw new Exception("未找到 OptionSettings 行");

            string optionLine = match.Groups[1].Value;
            var dict = ParseOptionSettings(optionLine);

            if (!dict.ContainsKey("AdminPassword") || string.IsNullOrEmpty(dict["AdminPassword"]))
                dict["AdminPassword"] = "1234";
            dict["RESTAPIEnabled"] = "True";

            // 重新构建 OptionSettings 行
            var pairList = new List<string>();
            foreach (var kv in dict)
            {
                string key = kv.Key;
                string val = kv.Value;

                // 特殊处理 DenyTechnologyList
                if (key.Equals("DenyTechnologyList", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(val))
                        pairList.Add($"{key}=");
                    else
                    {
                        if (!val.StartsWith("(") && !val.EndsWith(")"))
                            val = $"({val})";
                        pairList.Add($"{key}={val}");
                    }
                    continue;
                }

                bool needQuote = false;
                if (AlwaysQuoteKeys.Contains(key))
                    needQuote = true;
                else if (NeverQuoteKeys.Contains(key))
                    needQuote = false;
                else if (val.StartsWith("(") && val.EndsWith(")"))
                    needQuote = false;
                else if (Regex.IsMatch(val, @"^-?\d+(\.\d+)?$") ||
                         val.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                         val.Equals("false", StringComparison.OrdinalIgnoreCase))
                    needQuote = false;
                else
                    needQuote = true;

                if (string.IsNullOrEmpty(val))
                {
                    pairList.Add($"{key}=\"\"");
                    continue;
                }
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
            string configFile = FindConfigFile(exePath);
            if (string.IsNullOrEmpty(configFile)) return null;

            string content = File.ReadAllText(configFile);
            var match = Regex.Match(content, @"OptionSettings\s*=\s*\(([^)]*)\)");
            if (!match.Success) return null;

            var dict = ParseOptionSettings(match.Groups[1].Value);
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