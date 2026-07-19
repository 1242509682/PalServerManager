using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PalServerManager
{
    public class ConfigEditForm : Form
    {
        private SrvMgr mgr;
        private DataGridView dgvConfig;
        private Button btnSave, btnClose;
        private Dictionary<string, string> configPairs;
        private string configFilePath;
        private string optionSettingsLine;

        // 中文映射字典（完整）
        private static readonly Dictionary<string, string> CnEnMap = new()
        {
            ["游戏难度"] = "Difficulty",
            ["随机化类型"] = "RandomizerType",
            ["随机化种子"] = "RandomizerSeed",
            ["随机帕鲁等级"] = "bIsRandomizerPalLevelRandom",
            ["白天速度"] = "DayTimeSpeedRate",
            ["夜晚速度"] = "NightTimeSpeedRate",
            ["经验倍率"] = "ExpRate",
            ["捕获倍率"] = "PalCaptureRate",
            ["生成数量"] = "PalSpawnNumRate",
            ["攻击伤害"] = "PalDamageRateAttack",
            ["防御倍率"] = "PalDamageRateDefense",
            ["玩家攻击"] = "PlayerDamageRateAttack",
            ["玩家受伤"] = "PlayerDamageRateDefense",
            ["饱食消耗"] = "PlayerStomachDecreaceRate",
            ["体力消耗"] = "PlayerStaminaDecreaceRate",
            ["玩家回血"] = "PlayerAutoHPRegeneRate",
            ["睡眠回血"] = "PlayerAutoHpRegeneRateInSleep",
            ["帕鲁饱食"] = "PalStomachDecreaceRate",
            ["帕鲁体力"] = "PalStaminaDecreaceRate",
            ["帕鲁回血"] = "PalAutoHPRegeneRate",
            ["帕鲁睡眠回血"] = "PalAutoHpRegeneRateInSleep",
            ["建筑生命"] = "BuildObjectHpRate",
            ["建筑伤害"] = "BuildObjectDamageRate",
            ["建筑老化"] = "BuildObjectDeteriorationDamageRate",
            ["采集掉落"] = "CollectionDropRate",
            ["采集物生命"] = "CollectionObjectHpRate",
            ["采集刷新"] = "CollectionObjectRespawnSpeedRate",
            ["敌怪掉落"] = "EnemyDropItemRate",
            ["死亡惩罚"] = "DeathPenalty",
            ["玩家伤害开关"] = "bEnablePlayerToPlayerDamage",
            ["友军伤害"] = "bEnableFriendlyFire",
            ["入侵敌人"] = "bEnableInvaderEnemy",
            ["UNKO"] = "bActiveUNKO",
            ["手柄辅助"] = "bEnableAimAssistPad",
            ["键盘辅助"] = "bEnableAimAssistKeyboard",
            ["掉落物数量"] = "DropItemMaxNum",
            ["物理掉落物"] = "PhysicsActiveDropItemMaxNum",
            ["UNKO掉落"] = "DropItemMaxNum_UNKO",
            ["物品重量"] = "ItemWeightRate",
            ["最大据点"] = "BaseCampMaxNum",
            ["据点帕鲁数"] = "BaseCampWorkerMaxNum",
            ["掉落保留时间"] = "DropItemAliveMaxHours",
            ["自动重置公会"] = "bAutoResetGuildNoOnlinePlayers",
            ["重置时间"] = "AutoResetGuildTimeNoOnlinePlayers",
            ["公会人数"] = "GuildPlayerMaxNum",
            ["公会据点数"] = "BaseCampMaxNumInGuild",
            ["孵化时间"] = "PalEggDefaultHatchingTime",
            ["工作速度"] = "WorkSpeedRate",
            ["自动保存间隔"] = "AutoSaveSpan",
            ["使用备份存档"] = "bIsUseBackupSaveData",
            ["多人模式"] = "bIsMultiplay",
            ["PVP模式"] = "bIsPvP",
            ["硬核模式"] = "bHardcore",
            ["帕鲁永久死亡"] = "bPalLost",
            ["硬核重建角色"] = "bCharacterRecreateInHardcore",
            ["拾取其他公会掉落"] = "bCanPickupOtherGuildDeathPenaltyDrop",
            ["非登录惩罚"] = "bEnableNonLoginPenalty",
            ["快速旅行"] = "bEnableFastTravel",
            ["仅据点快速旅行"] = "bEnableFastTravelOnlyBaseCamp",
            ["地图出生点"] = "bIsStartLocationSelectByMap",
            ["登出保留玩家"] = "bExistPlayerAfterLogout",
            ["防御其他公会"] = "bEnableDefenseOtherGuildPlayer",
            ["隐藏公会区域特效"] = "bInvisibleOtherGuildBaseCampAreaFX",
            ["限制建造区域"] = "bBuildAreaLimit",
            ["合作玩家数"] = "CoopPlayerMaxNum",
            ["服务器最大玩家数"] = "ServerPlayerMaxNum",
            ["服务器名称"] = "ServerName",
            ["服务器描述"] = "ServerDescription",
            ["管理员密码"] = "AdminPassword",
            ["服务器密码"] = "ServerPassword",
            ["允许客户端模组"] = "bAllowClientMod",
            ["公开端口"] = "PublicPort",
            ["公网IP"] = "PublicIP",
            ["区域"] = "Region",
            ["启用验证"] = "bUseAuth",
            ["封禁列表URL"] = "BanListURL",
            ["聊天限制"] = "ChatPostLimitPerMinute",
            ["启用RCON"] = "RCONEnabled",
            ["RCON端口"] = "RCONPort",
            ["启用REST API"] = "RESTAPIEnabled",
            ["REST API端口"] = "RESTAPIPort",
            ["跨平台"] = "CrossplayPlatforms",
            ["显示玩家列表"] = "bShowPlayerList",
            ["日志格式"] = "LogFormatType",
            ["显示加入离开消息"] = "bIsShowJoinLeftMessage",
            ["启用语音聊天"] = "bEnableVoiceChat",
            ["语音最大距离"] = "VoiceChatMaxVolumeDistance",
            ["语音零距离"] = "VoiceChatZeroVolumeDistance",
            ["空投间隔"] = "SupplyDropSpan",
            ["掠食者Boss"] = "EnablePredatorBossPal",
            ["最大建筑限制"] = "MaxBuildingLimitNum",
            ["复制裁剪距离"] = "ServerReplicatePawnCullDistance",
            ["允许帕鲁箱导出"] = "bAllowGlobalPalboxExport",
            ["允许帕鲁箱导入"] = "bAllowGlobalPalboxImport",
            ["装备耐久损耗"] = "EquipmentDurabilityDamageRate",
            ["容器脏标记间隔"] = "ItemContainerForceMarkDirtyInterval",
            ["帕鲁存储检查间隔"] = "PlayerDataPalStorageUpdateCheckTickInterval",
            ["物品腐化倍率"] = "ItemCorruptionMultiplier",
            ["怪物农场速度"] = "MonsterFarmActionSpeedRate",
            ["禁用科技列表"] = "DenyTechnologyList",
            ["公会重新加入冷却"] = "GuildRejoinCooldownMinutes",
            ["自动转移管理员检查间隔"] = "AutoTransferMasterCheckIntervalSeconds",
            ["自动转移管理员阈值"] = "AutoTransferMasterThresholdDays",
            ["每帧公会数"] = "MaxGuildsPerFrame",
            ["方块重生时间"] = "BlockRespawnTime",
            ["重生惩罚持续时间阈值"] = "RespawnPenaltyDurationThreshold",
            ["重生惩罚时间缩放"] = "RespawnPenaltyTimeScale",
            ["显示PVP物品(据点)"] = "bDisplayPvPItemNumOnWorldMap_BaseCamp",
            ["显示PVP物品(玩家)"] = "bDisplayPvPItemNumOnWorldMap_Player",
            ["PVP击杀额外掉落物"] = "AdditionalDropItemWhenPlayerKillingInPvPMode",
            ["PVP击杀额外掉落数量"] = "AdditionalDropItemNumWhenPlayerKillingInPvPMode",
            ["启用PVP击杀额外掉落"] = "bAdditionalDropItemWhenPlayerKillingInPvPMode",
            ["允许强化生命"] = "bAllowEnhanceStat_Health",
            ["允许强化攻击"] = "bAllowEnhanceStat_Attack",
            ["允许强化耐力"] = "bAllowEnhanceStat_Stamina",
            ["允许强化负重"] = "bAllowEnhanceStat_Weight",
            ["允许强化工作速度"] = "bAllowEnhanceStat_WorkSpeed",
            ["显示建筑玩家ID"] = "bEnableBuildingPlayerUIdDisplay",
            ["建筑名称缓存TTL"] = "BuildingNameDisplayCacheTTLSeconds"
        };

        public ConfigEditForm(SrvMgr manager)
        {
            mgr = manager;
            string exePath = mgr.CurrentExePath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show("未检测到运行中的服务端或未设置服务端路径。\n请先启动或附加服务端，再打开配置编辑。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                return;
            }

            string win64Dir = Path.GetDirectoryName(exePath);
            string binariesDir = Path.GetDirectoryName(win64Dir);
            string palRoot = Path.GetDirectoryName(binariesDir);
            configFilePath = Path.Combine(palRoot, "Saved", "Config", "WindowsServer", "PalWorldSettings.ini");

            if (!File.Exists(configFilePath))
            {
                MessageBox.Show($"未找到配置文件：{configFilePath}\n请确认服务端目录结构完整。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            LoadConfig();
        }

        private void LoadConfig()
        {
            string content = File.ReadAllText(configFilePath);
            var match = Regex.Match(content, @"OptionSettings\s*=\s*\(([^)]*)\)");
            if (!match.Success)
            {
                MessageBox.Show("未找到 OptionSettings 行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            optionSettingsLine = match.Groups[1].Value;
            configPairs = ParseOptionSettings(optionSettingsLine);
            DisplayConfig();
        }

        private Dictionary<string, string> ParseOptionSettings(string line)
        {
            var dict = new Dictionary<string, string>();
            int i = 0;
            int len = line.Length;

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

        private string ReadValue(string line, ref int pos)
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
                string value = line.Substring(start, pos - start);
                pos++;
                return value;
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
                int valStart = pos;
                while (pos < len && line[pos] != ',')
                {
                    if (line[pos] == '(')
                    {
                        ReadValue(line, ref pos);
                        continue;
                    }
                    pos++;
                }
                return line.Substring(valStart, pos - valStart).TrimEnd();
            }
        }

        private void DisplayConfig()
        {
            dgvConfig.Rows.Clear();
            foreach (var pair in configPairs)
            {
                string cnName = CnEnMap.FirstOrDefault(x => x.Value == pair.Key).Key;
                if (string.IsNullOrEmpty(cnName))
                    cnName = pair.Key;
                dgvConfig.Rows.Add(cnName, pair.Value);
            }
            dgvConfig.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void InitializeComponent()
        {
            this.Text = "配置编辑";
            this.Size = new Size(650, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 244, 248);

            dgvConfig = new DataGridView
            {
                Location = new Point(12, 12),
                Size = new Size(560, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Font = new Font("微软雅黑", 9F),
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter
            };
            dgvConfig.Columns.Add("Name", "配置项");
            dgvConfig.Columns.Add("Value", "值");
            dgvConfig.Columns[0].Width = 200;
            dgvConfig.Columns[1].Width = 350;
            this.Controls.Add(dgvConfig);

            int y = dgvConfig.Bottom + 12;
            btnSave = new Button
            {
                Text = "保存配置",
                Location = new Point(12, y),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 100) },
                BackColor = Color.FromArgb(230, 245, 230),
                ForeColor = Color.FromArgb(0, 120, 0),
                Font = new Font("微软雅黑", 9F)
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(140, y),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            Label lblHint = new Label
            {
                Text = "提示：修改后点击保存，服务端需要重启才能生效。",
                AutoSize = true,
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                Location = new Point(280, y + 6)
            };
            this.Controls.Add(lblHint);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var updatedPairs = new Dictionary<string, string>();
            for (int i = 0; i < dgvConfig.Rows.Count; i++)
            {
                if (dgvConfig.Rows[i].IsNewRow) continue;
                string cnName = dgvConfig.Rows[i].Cells[0].Value?.ToString();
                string val = dgvConfig.Rows[i].Cells[1].Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(cnName)) continue;
                string enKey = CnEnMap.FirstOrDefault(x => x.Key == cnName).Value;
                if (string.IsNullOrEmpty(enKey))
                    enKey = cnName;
                updatedPairs[enKey] = val;
            }

            // 使用与 GameConfigHelper 相同的引号规则（直接复制上面的逻辑）
            var pairList = new List<string>();
            foreach (var kv in updatedPairs)
            {
                string key = kv.Key;
                string val = kv.Value;
                bool needQuote = false;

                if (GameConfigHelper.AlwaysQuoteKeys.Contains(key))
                    needQuote = true;
                else if (GameConfigHelper.NeverQuoteKeys.Contains(key))
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
            string content = File.ReadAllText(configFilePath);
            string pattern = @"(OptionSettings\s*=\s*\().*?(\))";
            string newContent = Regex.Replace(content, pattern, $"$1{newOptionLine}$2");

            try
            {
                File.WriteAllText(configFilePath, newContent);
                MessageBox.Show("配置已保存，请重启服务端生效。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}