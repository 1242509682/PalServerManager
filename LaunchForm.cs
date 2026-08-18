using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PalServerManager;

public class LaunchForm : Form
{
    private SrvMgr mgr;
    private Config cfg;
    private Ue4Mgr ue4;
    private TextBox txtExePath;
    private Button btnBrowse, btnAttach, btnStart, btnOpenDir, btnClose;

    public LaunchForm(SrvMgr manager, Config config, Ue4Mgr ue4Manager)
    {
        mgr = manager;
        cfg = config;
        ue4 = ue4Manager;
        InitializeComponent();
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;
        txtExePath.Text = cfg.SvrExe;
    }

    private void InitializeComponent()
    {
        this.Text = "启动服务 / 附加进程";
        this.Size = new Size(580, 220); // 缩小高度，因为移除了列表
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
            using var dlg = new OpenFileDialog
            {
                Filter = "PalServer 可执行文件|PalServer.exe|所有文件|*.*",
                Title = "选择 PalServer.exe",
                FileName = "PalServer.exe"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = dlg.FileName;
                if (Path.GetFileName(selectedFile).Equals("PalServer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    txtExePath.Text = selectedFile;
                }
                else
                {
                    MessageBox.Show("请选择名称为 PalServer.exe 的文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 不清除已有文本，但也不设置新路径
                }
            }
        };
        this.Controls.Add(btnBrowse);

        y += 50;

        // ---- 操作按钮 ----
        int btnW = 100, btnH = 32;
        int spacing = 10;

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
            // 自动查找并附加 PalServer-Win64-Shipping-Cmd 进程
            if (mgr.IsRun)
            {
                MessageBox.Show("已有服务端进程被管理，无需重复附加。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var procs = mgr.GetProcs();
            Process target = null;
            foreach (var p in procs)
            {
                // 进程名精确匹配（不包含 .exe）
                if (p.ProcessName == "PalServer-Win64-Shipping-Cmd")
                {
                    target = p;
                    break;
                }
            }

            if (target == null)
            {
                MessageBox.Show("未找到正在运行的 PalServer-Win64-Shipping-Cmd 进程。\n请确保服务端已启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (mgr.AttachProc(target))
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
            Location = new Point(left + btnW + spacing, y),
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
            mgr.EnsureGameConfig(txtExePath.Text);
            ue4?.SetWinDirFromExe(mgr.CurrentExePath);
            MessageBox.Show("服务端启动成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        };
        this.Controls.Add(btnStart);

        btnOpenDir = new Button
        {
            Text = "打开目录",
            Location = new Point(left + 2 * (btnW + spacing), y),
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

        btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(left + 3 * (btnW + spacing), y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = ctrlFont
        };
        btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(btnClose);
    }
}