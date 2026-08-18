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
        private CheckBox chkEnableMemMonitor;
        private NumericUpDown numMemoryThreshold;
        private NumericUpDown numMemoryCheckInterval;
        private Button btnSaveConfig, btnRestart, btnKill, btnInfo, btnMetrics, btnClose;

        public ControlForm(SrvMgr manager, Config config)
        {
            mgr = manager;
            cfg = config;
            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;

            // 加载配置值
            numRuntime.Value = cfg.RuntimeSeconds;
            numShutdownWait.Value = cfg.ShutdownWaittime > 0 ? cfg.ShutdownWaittime : 5;
            txtShutdownMsg.Text = cfg.ShutdownMessage;
            chkEnableMemMonitor.Checked = cfg.EnableMemoryMonitor;
            numMemoryThreshold.Value = cfg.MemoryThresholdMB > 0 ? cfg.MemoryThresholdMB : 1024;
            numMemoryCheckInterval.Value = cfg.MemoryCheckIntervalSeconds > 0 ? cfg.MemoryCheckIntervalSeconds : 60;
        }

        private void InitializeComponent()
        {
            this.Text = "服务器控制";
            this.Size = new Size(640, 580);
            this.MinimumSize = new Size(640, 580);
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
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ---- 配置区域 ----
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
                RowCount = 4,
                BackColor = Color.Transparent
            };
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            // 前三行固定高度，第四行自动
            configTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            configTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            configTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            configTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // ---- 行0：运行累计重启 ----
            configTable.Controls.Add(new Label
            {
                Text = "定时重启(秒):",
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

            // ---- 行1：关服等待 ----
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

            // ---- 行2：关服消息 + 保存按钮 ----
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

                cfg.EnableMemoryMonitor = chkEnableMemMonitor.Checked;
                cfg.MemoryThresholdMB = (int)numMemoryThreshold.Value;
                cfg.MemoryCheckIntervalSeconds = (int)numMemoryCheckInterval.Value;
                cfg.Save();

                MessageBox.Show($"配置已保存：\n运行累计重启 {runtime} 秒\n关服等待 {wait} 秒\n消息 \"{msg}\"\n内存监控 {(cfg.EnableMemoryMonitor ? "启用" : "禁用")}\n阈值 {cfg.MemoryThresholdMB} MB\n检查间隔 {cfg.MemoryCheckIntervalSeconds} 秒",
                    "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            configTable.Controls.Add(btnSaveConfig, 2, 2);

            // ---- 行3：内存监控（所有控件放在同一个 FlowLayoutPanel 中跨三列） ----
            var memPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 6) // 上下留白，垂直居中
            };

            // 标签
            memPanel.Controls.Add(new Label
            {
                Text = "剩余内存重启:",
                AutoSize = true,
                Font = lblFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Margin = new Padding(0, 4, 12, 0)
            });

            // 启用复选框
            chkEnableMemMonitor = new CheckBox
            {
                Text = "启用",
                AutoSize = true,
                Font = ctrlFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Margin = new Padding(0, 4, 12, 0)
            };
            memPanel.Controls.Add(chkEnableMemMonitor);

            // 阈值标签 + 数值
            memPanel.Controls.Add(new Label
            {
                Text = "阈值(MB):",
                AutoSize = true,
                Font = ctrlFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Margin = new Padding(0, 4, 4, 0)
            });
            numMemoryThreshold = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 32768,
                Value = 1024,
                Width = 80,
                Font = ctrlFont,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Margin = new Padding(0, 2, 12, 0)
            };
            memPanel.Controls.Add(numMemoryThreshold);

            // 间隔标签 + 数值
            memPanel.Controls.Add(new Label
            {
                Text = "间隔(秒):",
                AutoSize = true,
                Font = ctrlFont,
                ForeColor = Color.FromArgb(40, 60, 90),
                Margin = new Padding(0, 4, 4, 0)
            });
            numMemoryCheckInterval = new NumericUpDown
            {
                Minimum = 5,
                Maximum = 3600,
                Value = 60,
                Width = 80,
                Font = ctrlFont,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Margin = new Padding(0, 2, 0, 0)
            };
            memPanel.Controls.Add(numMemoryCheckInterval);

            // 将 memPanel 放置在第0列，跨3列
            configTable.Controls.Add(memPanel, 0, 3);
            configTable.SetColumnSpan(memPanel, 3);

            configGroup.Controls.Add(configTable);
            mainTable.Controls.Add(configGroup, 0, 0);

            // ---- 操作区域 ----
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
            btnTable.Controls.Add(btnClose, 2, 0);

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

            actionGroup.Controls.Add(btnTable);
            mainTable.Controls.Add(actionGroup, 0, 1);

            this.Controls.Add(mainTable);
        }
    }
}