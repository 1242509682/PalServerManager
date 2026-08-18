using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager
{
    public partial class FrmMain : Form
    {
        private SrvMgr mgr;
        private Config cfg;
        private Ue4Mgr ue4;
        private Timer uiTmr;

        private RichTextBox rtbLog;
        private StatusStrip stBar;
        private ToolStripStatusLabel lblServerState, lblUe4State, lblStatusInfo;
        private Button btnLaunch, btnControl, btnBroadcast, btnPunish, btnUe4, btnConfigEdit;

        private ToolTip toolTip1;

        private Button btnMod;

        public FrmMain()
        {
            cfg = Config.Load();
            mgr = new SrvMgr(cfg);

            try { ue4 = new Ue4Mgr(cfg); }
            catch (Exception) { /* 忽略 */ }

            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;

            toolTip1 = new ToolTip();
            toolTip1.SetToolTip(btnControl, "服务端未运行");
            toolTip1.SetToolTip(btnBroadcast, "服务端未运行");
            toolTip1.SetToolTip(btnPunish, "服务端未运行");

            mgr.OnLog += AppendLog;
            if (ue4 != null) ue4.OnLog += AppendLog;

            // ---- 初始化 UE4SS 路径（不再强制同步配置，避免启动弹窗） ----
            if (ue4 != null)
            {
                bool pathSet = false;
                if (!string.IsNullOrEmpty(cfg.SvrExe) && File.Exists(cfg.SvrExe))
                {
                    // 只设置路径，不调用 EnsureGameConfig（该函数可能弹窗）
                    pathSet = ue4.SetWinDirFromExe(cfg.SvrExe);
                    if (pathSet)
                    {
                        // 静默检查配置文件是否存在，仅记录日志
                        string configPath = GameConfigHelper.FindConfigFile(cfg.SvrExe);
                        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                            AppendLog("未找到 PalWorldSettings.ini，启动服务时将自动处理。");
                        else
                            AppendLog("已找到配置文件，启动服务时会自动同步密码。");
                    }
                }
                if (!pathSet)
                {
                    AppendLog("配置路径无效，尝试自动搜索 UE4SS...");
                    if (ue4.AutoDetect())
                        AppendLog("自动搜索定位到 UE4SS 目录。");
                    else
                        AppendLog("未找到 UE4SS 安装，请通过“启动服务”指定路径后再安装。");
                }
            }

            btnLaunch.Click += (s, e) => new LaunchForm(mgr, cfg, ue4).ShowDialog(this);
            btnControl.Click += (s, e) => new ControlForm(mgr, cfg).ShowDialog(this);
            btnBroadcast.Click += (s, e) => new BroadcastForm(mgr).ShowDialog(this);
            btnPunish.Click += (s, e) => new PunishForm(mgr, cfg).ShowDialog(this);
            btnUe4.Click += (s, e) => new Ue4Form(ue4, mgr).ShowDialog(this);
            btnConfigEdit.Click += (s, e) => new ConfigEditForm(mgr).ShowDialog(this);
            btnMod.Click += (s, e) =>
            {
                var modMgr = new ModManager(cfg, ue4);
                new ModForm(modMgr).ShowDialog(this);
            };

            uiTmr = new Timer { Interval = cfg.UiRefreshInterval };
            uiTmr.Tick += (s, e) =>
            {
                bool run = mgr.IsRun;
                lblServerState.Text = run ? "● 运行中" : "○ 已停止";
                lblServerState.ForeColor = run ? Color.Green : Color.Red;

                bool ue4Ready = ue4 != null && ue4.IsReady;
                lblUe4State.Text = ue4Ready ? (ue4.IsInstalled() ? "UE4SS 已安装" : "UE4SS 未安装") : "UE4SS 不可用";
                lblUe4State.ForeColor = ue4Ready ? (ue4.IsInstalled() ? Color.Green : Color.Orange) : Color.Gray;

                btnControl.Enabled = run;
                btnBroadcast.Enabled = run;
                btnPunish.Enabled = run;

                if (!run)
                {
                    toolTip1.SetToolTip(btnControl, "服务端未运行");
                    toolTip1.SetToolTip(btnBroadcast, "服务端未运行");
                    toolTip1.SetToolTip(btnPunish, "服务端未运行");
                }
                else
                {
                    toolTip1.SetToolTip(btnControl, "");
                    toolTip1.SetToolTip(btnBroadcast, "");
                    toolTip1.SetToolTip(btnPunish, "");
                }
            };
            uiTmr.Start();

            this.FormClosing += (s, e) =>
            {
                uiTmr?.Stop();
                mgr?.TerminateNow();
            };

            AppendLog("主界面已加载。点击对应按钮管理服务器。");

            bool initialRun = mgr.IsRun;
            btnControl.Enabled = initialRun;
            btnBroadcast.Enabled = initialRun;
            btnPunish.Enabled = initialRun;
            if (!initialRun)
            {
                toolTip1.SetToolTip(btnControl, "服务端未运行");
                toolTip1.SetToolTip(btnBroadcast, "服务端未运行");
                toolTip1.SetToolTip(btnPunish, "服务端未运行");
            }
        }

        private void AppendLog(string msg)
        {
            if (rtbLog == null) return;
            if (rtbLog.InvokeRequired) { rtbLog.Invoke(new Action<string>(AppendLog), msg); return; }
            Color color = Color.Black;
            if (msg.Contains("[UE4SS]")) color = Color.Purple;
            else if (msg.Contains("错误") || msg.Contains("失败") || msg.Contains("找不到")) color = Color.Red;
            else if (msg.Contains("成功") || msg.Contains("完成") || msg.Contains("已启动")) color = Color.Green;
            else if (msg.Contains("警告")) color = Color.Orange;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText(msg + Environment.NewLine);
            rtbLog.ScrollToCaret();
        }

        private void InitializeComponent()
        {
            this.Text = "Palworld 服务器管理器 by 羽学 QQ1242509682";
            this.Size = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 244, 248);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(850, 500);

            rtbLog = new RichTextBox
            {
                Location = new Point(12, 12),
                Size = new Size(this.ClientSize.Width - 24, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            rtbLog.ContextMenuStrip = CreateLogContextMenu();
            this.Controls.Add(rtbLog);

            int btnWidth = 125, btnHeight = 34;
            int spacing = 6;
            int totalWidth = btnWidth * 7 + spacing * 6; // 改为7个按钮
            int startX = (this.ClientSize.Width - totalWidth) / 2;
            int y = rtbLog.Bottom + 15;

            btnLaunch = new Button
            {
                Text = "🚀 启动服务",
                Location = new Point(startX, y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
                BackColor = Color.FromArgb(225, 240, 255),
                ForeColor = Color.FromArgb(0, 80, 180),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnLaunch);

            btnControl = new Button
            {
                Text = "⚙️ 服务器控制",
                Location = new Point(startX + btnWidth + spacing, y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 180, 0) },
                BackColor = Color.FromArgb(255, 245, 225),
                ForeColor = Color.FromArgb(180, 100, 0),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnControl);

            btnBroadcast = new Button
            {
                Text = "📢 广播",
                Location = new Point(startX + 2 * (btnWidth + spacing), y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 200) },
                BackColor = Color.FromArgb(225, 245, 245),
                ForeColor = Color.FromArgb(0, 100, 120),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnBroadcast);

            btnPunish = new Button
            {
                Text = "🔨 惩罚",
                Location = new Point(startX + 3 * (btnWidth + spacing), y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
                BackColor = Color.FromArgb(255, 235, 225),
                ForeColor = Color.FromArgb(180, 60, 0),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnPunish);

            btnUe4 = new Button
            {
                Text = "🔧 UE4SS",
                Location = new Point(startX + 4 * (btnWidth + spacing), y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 150, 255) },
                BackColor = Color.FromArgb(240, 230, 255),
                ForeColor = Color.FromArgb(100, 50, 160),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnUe4);

            btnConfigEdit = new Button
            {
                Text = "📝 配置编辑",
                Location = new Point(startX + 5 * (btnWidth + spacing), y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 200) },
                BackColor = Color.FromArgb(225, 245, 245),
                ForeColor = Color.FromArgb(0, 100, 120),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnConfigEdit);

            // 新增 btnMod
            btnMod = new Button
            {
                Text = "📦 MOD管理",
                Location = new Point(startX + 6 * (btnWidth + spacing), y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 100) },
                BackColor = Color.FromArgb(230, 245, 230),
                ForeColor = Color.FromArgb(0, 120, 0),
                Font = new Font("微软雅黑", 9F)
            };
            this.Controls.Add(btnMod);

            stBar = new StatusStrip
            {
                BackColor = Color.FromArgb(230, 235, 240),
                SizingGrip = false
            };
            lblServerState = new ToolStripStatusLabel("○ 已停止")
            {
                ForeColor = Color.Red,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblUe4State = new ToolStripStatusLabel("UE4SS 未安装")
            {
                ForeColor = Color.Gray,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblStatusInfo = new ToolStripStatusLabel("就绪")
            {
                ForeColor = Color.DimGray,
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            stBar.Items.Add(lblServerState);
            stBar.Items.Add(lblUe4State);
            stBar.Items.Add(lblStatusInfo);
            this.Controls.Add(stBar);
        }

        private ContextMenuStrip CreateLogContextMenu()
        {
            var menu = new ContextMenuStrip();
            var copyItem = new ToolStripMenuItem("复制日志", null, (s, e) =>
            {
                if (!string.IsNullOrEmpty(rtbLog.Text))
                    Clipboard.SetText(rtbLog.Text);
            });
            var clearItem = new ToolStripMenuItem("清空日志", null, (s, e) => rtbLog.Clear());
            menu.Items.Add(copyItem);
            menu.Items.Add(clearItem);
            return menu;
        }
    }
}