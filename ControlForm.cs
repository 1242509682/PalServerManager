using System;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager
{
    public class ControlForm : Form
    {
        private SrvMgr mgr;
        private Config cfg;
        private NumericUpDown numRuntime;
        private NumericUpDown numShutdownWait;
        private TextBox txtShutdownMsg;
        private Button btnSaveConfig, btnRestart, btnKill, btnEditConfig, btnInfo, btnMetrics, btnClose;

        public ControlForm(SrvMgr manager, Config config)
        {
            mgr = manager;
            cfg = config;
            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            numRuntime.Value = cfg.RuntimeSeconds;
            numShutdownWait.Value = cfg.ShutdownWaittime > 0 ? cfg.ShutdownWaittime : 5;
            txtShutdownMsg.Text = cfg.ShutdownMessage;
        }

        private void InitializeComponent()
        {
            this.Text = "服务器控制";
            this.Size = new Size(580, 480);
            this.MinimumSize = new Size(580, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 244, 248);

            Font lblFont = new Font("微软雅黑", 9F, FontStyle.Bold);
            Font ctrlFont = new Font("微软雅黑", 9F);

            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var configGroup = new GroupBox
            {
                Text = "⚙️ 服务器配置",
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 90),
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8)
            };

            var configTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            for (int i = 0; i < 3; i++)
                configTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

            configTable.Controls.Add(new Label
            {
                Text = "运行累计重启(秒):",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = lblFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Dock = DockStyle.Fill
            }, 0, 0);

            numRuntime = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 86400,
                Value = 0,
                Dock = DockStyle.Fill,
                Font = ctrlFont,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90)
            };
            configTable.Controls.Add(numRuntime, 1, 0);

            configTable.Controls.Add(new Label
            {
                Text = "关服等待(秒):",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = lblFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Dock = DockStyle.Fill
            }, 0, 1);

            numShutdownWait = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 300,
                Value = 5,
                Dock = DockStyle.Fill,
                Font = ctrlFont,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90)
            };
            configTable.Controls.Add(numShutdownWait, 1, 1);

            configTable.Controls.Add(new Label
            {
                Text = "关服消息:",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = lblFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Dock = DockStyle.Fill
            }, 0, 2);

            txtShutdownMsg = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = ctrlFont,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90)
            };
            configTable.Controls.Add(txtShutdownMsg, 1, 2);

            btnSaveConfig = new Button
            {
                Text = "保存配置",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 100) },
                BackColor = Color.FromArgb(230, 245, 230),
                ForeColor = Color.FromArgb(0, 120, 0),
                Font = ctrlFont
            };
            btnSaveConfig.Click += (s, e) =>
            {
                int runtime = (int)numRuntime.Value;
                mgr.SetRuntime(runtime);

                int wait = (int)numShutdownWait.Value;
                string msg = txtShutdownMsg.Text.Trim();
                mgr.UpdateShutdownConfig(wait, msg);

                MessageBox.Show($"配置已保存：\n运行累计重启 {runtime} 秒\n关服等待 {wait} 秒\n消息 \"{msg}\"",
                    "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            configTable.Controls.Add(btnSaveConfig, 2, 2);

            configGroup.Controls.Add(configTable);
            mainTable.Controls.Add(configGroup, 0, 0);

            var actionGroup = new GroupBox
            {
                Text = "🔧 服务器操作",
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 90),
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 8)
            };

            var btnTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            btnTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            btnTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            btnTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            btnTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            btnTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            int btnW = 130, btnH = 36;

            btnRestart = new Button
            {
                Text = "🔄 关服重启",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 180, 0) },
                BackColor = Color.FromArgb(255, 245, 225),
                ForeColor = Color.FromArgb(180, 100, 0),
                Font = ctrlFont
            };
            btnRestart.Click += async (s, e) =>
            {
                await mgr.ShutdownRst();
                MessageBox.Show("已发送关服命令，服务端将自动重启。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            btnTable.Controls.Add(btnRestart, 0, 0);

            btnKill = new Button
            {
                Text = "⛔ 强制停止",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
                BackColor = Color.FromArgb(255, 235, 225),
                ForeColor = Color.FromArgb(180, 60, 0),
                Font = ctrlFont
            };
            btnKill.Click += async (s, e) =>
            {
                if (MessageBox.Show("确认强制停止服务端？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    await mgr.KillSvrAsync();
                    MessageBox.Show("服务端已强制停止。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            btnTable.Controls.Add(btnKill, 1, 0);

            btnEditConfig = new Button
            {
                Text = "📝 配置编辑",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 200) },
                BackColor = Color.FromArgb(225, 245, 245),
                ForeColor = Color.FromArgb(0, 100, 120),
                Font = ctrlFont
            };
            btnEditConfig.Click += (s, e) =>
            {
                if (!mgr.IsRun)
                {
                    MessageBox.Show("服务端未运行，无法编辑配置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                new ConfigEditForm(mgr).ShowDialog(this);
            };
            btnTable.Controls.Add(btnEditConfig, 2, 0);

            btnInfo = new Button
            {
                Text = "📊 服务器信息",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(150, 200, 255) },
                BackColor = Color.FromArgb(235, 245, 255),
                ForeColor = Color.FromArgb(0, 60, 150),
                Font = ctrlFont
            };
            btnInfo.Click += async (s, e) =>
            {
                if (!mgr.IsRun) { MessageBox.Show("服务端未运行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string json = await mgr.SendApiGet("info");
                if (string.IsNullOrEmpty(json)) { MessageBox.Show("获取信息失败。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                new InfoForm("服务器信息", json).ShowDialog(this);
            };
            btnTable.Controls.Add(btnInfo, 0, 1);

            btnMetrics = new Button
            {
                Text = "📈 服务器指标",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 200, 255) },
                BackColor = Color.FromArgb(245, 245, 255),
                ForeColor = Color.FromArgb(80, 80, 150),
                Font = ctrlFont
            };
            btnMetrics.Click += async (s, e) =>
            {
                if (!mgr.IsRun) { MessageBox.Show("服务端未运行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string json = await mgr.SendApiGet("metrics");
                if (string.IsNullOrEmpty(json)) { MessageBox.Show("获取指标失败。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                new InfoForm("服务器指标", json).ShowDialog(this);
            };
            btnTable.Controls.Add(btnMetrics, 1, 1);

            btnClose = new Button
            {
                Text = "关闭",
                Size = new Size(btnW, btnH),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = ctrlFont
            };
            btnClose.Click += (s, e) => this.Close();
            btnTable.Controls.Add(btnClose, 2, 1);

            actionGroup.Controls.Add(btnTable);
            mainTable.Controls.Add(actionGroup, 0, 1);

            this.Controls.Add(mainTable);
        }
    }
}