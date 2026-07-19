using System;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager
{
    public class UnbanForm : Form
    {
        private SrvMgr mgr;
        private Config cfg;
        private DataGridView dgvBanned;
        private Button btnUnban, btnClose;

        public UnbanForm(SrvMgr manager, Config config)
        {
            mgr = manager;
            cfg = config;
            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            RefreshList();
        }

        private void RefreshList()
        {
            dgvBanned.Rows.Clear();
            foreach (var entry in cfg.BanList)
            {
                dgvBanned.Rows.Add(
                    entry.UserId,
                    entry.Name,
                    entry.IP,
                    entry.Level,
                    entry.BanTime.ToString("yyyy-MM-dd HH:mm:ss")
                );
            }
            if (dgvBanned.Rows.Count > 0)
                dgvBanned.Rows[0].Selected = true;
            dgvBanned.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void InitializeComponent()
        {
            this.Text = "封禁列表 - 解封";
            this.Size = new Size(700, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 350);
            this.BackColor = Color.FromArgb(240, 244, 248);

            Font ctrlFont = new Font("微软雅黑", 9F);

            int left = 16;
            int y = 16;

            Label lbl = new Label
            {
                Text = "已封禁玩家列表:",
                AutoSize = true,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 90),
                Location = new Point(left, y + 4)
            };
            this.Controls.Add(lbl);

            dgvBanned = new DataGridView
            {
                Location = new Point(left, y + 30),
                Size = new Size(660, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Font = ctrlFont,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false
            };
            // 定义列
            dgvBanned.Columns.Add("UserId", "UserID");
            dgvBanned.Columns.Add("Name", "玩家名");
            dgvBanned.Columns.Add("IP", "IP地址");
            dgvBanned.Columns.Add("Level", "等级");
            dgvBanned.Columns.Add("BanTime", "封禁时间");
            // 设置列宽
            dgvBanned.Columns[0].Width = 150;
            dgvBanned.Columns[1].Width = 120;
            dgvBanned.Columns[2].Width = 120;
            dgvBanned.Columns[3].Width = 60;
            dgvBanned.Columns[4].Width = 150;

            this.Controls.Add(dgvBanned);

            y += 330;

            int btnW = 100, btnH = 30;
            btnUnban = new Button
            {
                Text = "解封选中",
                Location = new Point(left, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 100) },
                BackColor = Color.FromArgb(230, 245, 230),
                ForeColor = Color.FromArgb(0, 120, 0),
                Font = ctrlFont
            };
            btnUnban.Click += async (s, e) =>
            {
                if (dgvBanned.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请选择要解封的玩家。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string userId = dgvBanned.SelectedRows[0].Cells["UserId"].Value?.ToString();
                if (string.IsNullOrEmpty(userId)) return;
                bool ok = await mgr.SendApi("unban", $"{{\"userid\":\"{userId}\"}}");
                if (ok)
                {
                    // 从列表中移除
                    var entry = cfg.BanList.Find(b => b.UserId == userId);
                    if (entry != null)
                    {
                        cfg.BanList.Remove(entry);
                        cfg.Save();
                    }
                    RefreshList();
                    MessageBox.Show("解封成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show("解封失败。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            this.Controls.Add(btnUnban);

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(left + btnW + 12, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = ctrlFont
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }
    }
}