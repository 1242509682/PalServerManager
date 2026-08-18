using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using static PalServerManager.Config;

namespace PalServerManager
{
    public class PunishForm : Form
    {
        private SrvMgr mgr;
        private Config cfg;
        private DataGridView dgvPlayers;
        private Button btnRefresh, btnKick, btnBan, btnUnbanList, btnClose;
        private Panel bottomPanel;

        public class PlayerInfo
        {
            [JsonProperty("userId")]
            public string UserId { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("accountName")]
            public string AccountName { get; set; }
            [JsonProperty("playerId")]
            public string PlayerId { get; set; }
            [JsonProperty("ip")]
            public string IP { get; set; }
            [JsonProperty("ping")]
            public double Ping { get; set; }
            [JsonProperty("location_x")]
            public double LocationX { get; set; }
            [JsonProperty("location_y")]
            public double LocationY { get; set; }
            [JsonProperty("level")]
            public int Level { get; set; }
        }

        public class PlayersResponse
        {
            [JsonProperty("players")]
            public List<PlayerInfo> Players { get; set; }
        }

        public PunishForm(SrvMgr manager, Config config)
        {
            mgr = manager;
            cfg = config;
            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            RefreshPlayers();
        }

        private void InitializeComponent()
        {
            this.Text = "玩家管理 (踢出/封禁)";
            this.Size = new Size(750, 500);
            this.MinimumSize = new Size(700, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(240, 244, 248);

            Font ctrlFont = new Font("微软雅黑", 9F);

            // ---- DataGridView（填充剩余空间） ----
            dgvPlayers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Font = ctrlFont,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false
            };
            var colUserId = new DataGridViewTextBoxColumn { Name = "UserId", HeaderText = "UserID", DataPropertyName = "UserId" };
            var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "玩家名", DataPropertyName = "Name" };
            var colIP = new DataGridViewTextBoxColumn { Name = "IP", HeaderText = "IP地址", DataPropertyName = "IP" };
            var colPing = new DataGridViewTextBoxColumn { Name = "Ping", HeaderText = "延迟(ms)", DataPropertyName = "Ping" };
            var colLevel = new DataGridViewTextBoxColumn { Name = "Level", HeaderText = "等级", DataPropertyName = "Level" };
            var colLocX = new DataGridViewTextBoxColumn { Name = "LocationX", HeaderText = "坐标 X", DataPropertyName = "LocationX" };
            var colLocY = new DataGridViewTextBoxColumn { Name = "LocationY", HeaderText = "坐标 Y", DataPropertyName = "LocationY" };
            var colAccountName = new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "AccountName", DataPropertyName = "AccountName", Visible = false };
            var colPlayerId = new DataGridViewTextBoxColumn { Name = "PlayerId", HeaderText = "PlayerId", DataPropertyName = "PlayerId", Visible = false };
            dgvPlayers.Columns.AddRange(colUserId, colName, colIP, colPing, colLevel, colLocX, colLocY, colAccountName, colPlayerId);

            // ---- 底部按钮面板 ----
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            int btnW = 90, btnH = 30;
            int left = 12;
            int y = 10;

            btnRefresh = new Button
            {
                Text = "刷新列表",
                Location = new Point(left, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 200) },
                BackColor = Color.FromArgb(225, 245, 245),
                ForeColor = Color.FromArgb(0, 100, 120),
                Font = ctrlFont
            };
            btnRefresh.Click += (s, e) => RefreshPlayers();
            bottomPanel.Controls.Add(btnRefresh);

            btnKick = new Button
            {
                Text = "踢出",
                Location = new Point(left + btnW + 8, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 180, 0) },
                BackColor = Color.FromArgb(255, 245, 225),
                ForeColor = Color.FromArgb(180, 100, 0),
                Font = ctrlFont
            };
            btnKick.Click += async (s, e) =>
            {
                if (dgvPlayers.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择一个玩家。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string userId = dgvPlayers.SelectedRows[0].Cells["UserId"].Value?.ToString();
                if (string.IsNullOrEmpty(userId)) return;
                var payload = new { userid = userId, message = "你被管理员踢出" };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                bool ok = await mgr.SendApi("kick", json);
                MessageBox.Show(ok ? "踢出成功。" : "踢出失败。", ok ? "成功" : "失败", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                if (ok) RefreshPlayers();
            };
            bottomPanel.Controls.Add(btnKick);

            btnBan = new Button
            {
                Text = "封禁",
                Location = new Point(left + 2 * (btnW + 8), y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
                BackColor = Color.FromArgb(255, 235, 225),
                ForeColor = Color.FromArgb(180, 60, 0),
                Font = ctrlFont
            };
            btnBan.Click += async (s, e) =>
            {
                if (dgvPlayers.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择一个玩家。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var row = dgvPlayers.SelectedRows[0];
                string userId = row.Cells["UserId"].Value?.ToString();
                if (string.IsNullOrEmpty(userId)) return;
                if (MessageBox.Show($"确认封禁玩家 {userId}？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    var payload = new { userid = userId, message = "你已被封禁" };
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    bool ok = await mgr.SendApi("ban", json);
                    if (ok)
                    {
                        if (!cfg.BanList.Any(b => b.UserId == userId))
                        {
                            var entry = new BanEntry
                            {
                                UserId = userId,
                                Name = row.Cells["Name"].Value?.ToString() ?? "",
                                IP = row.Cells["IP"].Value?.ToString() ?? "",
                                Level = int.TryParse(row.Cells["Level"].Value?.ToString(), out int lv) ? lv : 0,
                                BanTime = DateTime.Now
                            };
                            cfg.BanList.Add(entry);
                            cfg.Save();
                        }
                        MessageBox.Show("封禁成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshPlayers();
                    }
                    else
                        MessageBox.Show("封禁失败。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            bottomPanel.Controls.Add(btnBan);

            btnUnbanList = new Button
            {
                Text = "管理封禁列表",
                Location = new Point(left + 3 * (btnW + 8), y),
                Size = new Size(120, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 150, 255) },
                BackColor = Color.FromArgb(240, 230, 255),
                ForeColor = Color.FromArgb(100, 50, 160),
                Font = ctrlFont
            };
            btnUnbanList.Click += (s, e) => new UnbanForm(mgr, cfg).ShowDialog(this);
            bottomPanel.Controls.Add(btnUnbanList);

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(left + 4 * (btnW + 8) + 30, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = ctrlFont
            };
            btnClose.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnClose);

            this.Controls.Add(dgvPlayers);
            this.Controls.Add(bottomPanel);
        }

        private async void RefreshPlayers()
        {
            if (!mgr.IsRun)
            {
                dgvPlayers.DataSource = null;
                dgvPlayers.Rows.Clear();
                return;
            }

            string json = await mgr.SendApiGet("players");
            if (string.IsNullOrEmpty(json))
            {
                dgvPlayers.DataSource = null;
                dgvPlayers.Rows.Clear();
                return;
            }

            try
            {
                var response = JsonConvert.DeserializeObject<PlayersResponse>(json);
                if (response != null && response.Players != null && response.Players.Count > 0)
                {
                    dgvPlayers.DataSource = response.Players;
                    dgvPlayers.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                    int totalWidth = dgvPlayers.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
                    int newWidth = totalWidth + dgvPlayers.RowHeadersWidth + 40;
                    if (newWidth < this.MinimumSize.Width) newWidth = this.MinimumSize.Width;
                    this.Width = newWidth;
                }
                else
                {
                    dgvPlayers.DataSource = null;
                    dgvPlayers.Rows.Clear();
                }
            }
            catch (Exception ex)
            {
                dgvPlayers.DataSource = null;
                dgvPlayers.Rows.Clear();
                MessageBox.Show($"解析玩家数据失败：{ex.Message}\n原始数据：{json}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}