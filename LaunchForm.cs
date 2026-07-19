using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager;

public class LaunchForm : Form
{
    private SrvMgr mgr;
    private Config cfg;
    private Ue4Mgr ue4;
    private TextBox txtExePath;
    private Button btnBrowse, btnAttach, btnStart, btnOpenDir;
    private ListBox lstProcesses;
    private Button btnRefresh;

    public LaunchForm(SrvMgr manager, Config config, Ue4Mgr ue4Manager)
    {
        mgr = manager;
        cfg = config;
        ue4 = ue4Manager;
        InitializeComponent();
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;
        txtExePath.Text = cfg.SvrExe;
        RefreshProcessList();
    }

    private void RefreshProcessList()
    {
        lstProcesses.Items.Clear();
        var procs = mgr.GetProcs();
        foreach (var p in procs)
            lstProcesses.Items.Add($"PID:{p.Id}  {p.ProcessName}");
        if (lstProcesses.Items.Count > 0)
            lstProcesses.SelectedIndex = 0;
    }

    private void InitializeComponent()
    {
        this.Text = "启动服务 / 附加进程";
        this.Size = new Size(580, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 244, 248);

        Font lblFont = new Font("微软雅黑", 9F, FontStyle.Bold);
        Font ctrlFont = new Font("微软雅黑", 9F);

        int y = 16;
        int left = 16;

        // ---- 路径选择 ----
        Label lblExe = new Label
        {
            Text = "服务端 EXE:",
            AutoSize = true,
            Font = lblFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left, y + 4)
        };
        this.Controls.Add(lblExe);

        txtExePath = new TextBox
        {
            Location = new Point(left + 100, y),
            Size = new Size(340, 23),
            Font = ctrlFont,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90)
        };
        this.Controls.Add(txtExePath);

        btnBrowse = new Button
        {
            Text = "浏览",
            Location = new Point(left + 450, y - 1),
            Size = new Size(80, 35),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = ctrlFont
        };
        btnBrowse.Click += (s, e) =>
        {
            using var dlg = new OpenFileDialog { Filter = "可执行文件|*.exe", Title = "选择 PalServer.exe" };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtExePath.Text = dlg.FileName;
        };
        this.Controls.Add(btnBrowse);

        y += 40;

        // ---- 附加进程列表 ----
        Label lblProcs = new Label
        {
            Text = "运行中的服务端进程:",
            AutoSize = true,
            Font = lblFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left, y + 4)
        };
        this.Controls.Add(lblProcs);

        lstProcesses = new ListBox
        {
            Location = new Point(left, y + 28),
            Size = new Size(420, 160),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = ctrlFont,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90)
        };
        this.Controls.Add(lstProcesses);

        btnRefresh = new Button
        {
            Text = "刷新",
            Location = new Point(left + 430, y + 28),
            Size = new Size(80, 35),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 200) },
            BackColor = Color.FromArgb(225, 245, 245),
            ForeColor = Color.FromArgb(0, 100, 120),
            Font = ctrlFont
        };
        btnRefresh.Click += (s, e) => RefreshProcessList();
        this.Controls.Add(btnRefresh);

        y += 200;

        // ---- 按钮 ----
        int btnW = 100, btnH = 32;
        btnAttach = new Button
        {
            Text = "附加进程",
            Location = new Point(left, y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 180, 0) },
            BackColor = Color.FromArgb(255, 245, 225),
            ForeColor = Color.FromArgb(180, 100, 0),
            Font = ctrlFont
        };
        btnAttach.Click += (s, e) =>
        {
            if (lstProcesses.SelectedIndex < 0)
            {
                MessageBox.Show("请先选择一个进程。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var procs = mgr.GetProcs();
            var proc = procs[lstProcesses.SelectedIndex];
            if (mgr.AttachProc(proc))
            {
                ue4?.SetWinDirFromExe(mgr.CurrentExePath);
                mgr.EnsureGameConfig(mgr.CurrentExePath);
                MessageBox.Show("附加成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
                MessageBox.Show("附加失败。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        this.Controls.Add(btnAttach);

        btnStart = new Button
        {
            Text = "启动服务",
            Location = new Point(left + btnW + 10, y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = ctrlFont
        };
        btnStart.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(txtExePath.Text) || !System.IO.File.Exists(txtExePath.Text))
            {
                MessageBox.Show("请选择有效的服务端 EXE 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            mgr.SetExe(txtExePath.Text);
            mgr.StartSvr();
            mgr.EnsureGameConfig(txtExePath.Text); // 确保配置正确
            ue4?.SetWinDirFromExe(mgr.CurrentExePath);
            MessageBox.Show("服务端启动成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        };
        this.Controls.Add(btnStart);

        btnOpenDir = new Button
        {
            Text = "打开目录",
            Location = new Point(left + 2 * (btnW + 10), y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 200) },
            BackColor = Color.FromArgb(240, 240, 245),
            ForeColor = Color.FromArgb(60, 60, 100),
            Font = ctrlFont
        };
        btnOpenDir.Click += (s, e) =>
        {
            string dir = cfg.WorkDir;
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
            {
                string exePath = txtExePath.Text.Trim();
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                    dir = System.IO.Path.GetDirectoryName(exePath);
                else
                    dir = AppDomain.CurrentDomain.BaseDirectory;
            }
            if (System.IO.Directory.Exists(dir))
            {
                try { Process.Start("explorer.exe", dir); }
                catch { MessageBox.Show("无法打开目录。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            else
                MessageBox.Show("目录不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        this.Controls.Add(btnOpenDir);

        Button btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(left + 3 * (btnW + 10), y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = ctrlFont
        };
        btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(btnClose);

        // 自动刷新进程列表
        Timer refreshTimer = new Timer { Interval = cfg.LaunchRefreshInterval > 0 ? cfg.LaunchRefreshInterval : 2000 };
        refreshTimer.Tick += (s, e) => RefreshProcessList();
        refreshTimer.Start();
        this.FormClosed += (s, e) => refreshTimer.Stop();
    }
}