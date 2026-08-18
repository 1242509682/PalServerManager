using System;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager;

public class Ue4Form : Form
{
    private Ue4Mgr ue4;
    private SrvMgr mgr;
    private Button btnInstall, btnUninstall, btnOpenDir;
    private Label lblStatus;

    public Ue4Form(Ue4Mgr manager, SrvMgr serverManager)
    {
        ue4 = manager;
        mgr = serverManager;
        InitializeComponent();
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (ue4 == null)
            lblStatus.Text = "UE4SS 未初始化";
        else if (!ue4.IsReady)
            lblStatus.Text = "UE4SS 不可用（请先选择服务端路径）";
        else if (ue4.IsInstalled())
            lblStatus.Text = "UE4SS 已安装";
        else
            lblStatus.Text = "UE4SS 未安装";

        btnOpenDir.Enabled = (ue4 != null && ue4.IsReady && ue4.IsInstalled());
    }

    private void InitializeComponent()
    {
        this.Text = "UE4SS 管理";
        this.Size = new Size(500, 200);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 244, 248);

        Font ctrlFont = new Font("微软雅黑", 9F);

        int left = 20;
        int y = 20;

        lblStatus = new Label
        {
            Text = "状态",
            AutoSize = true,
            Font = new Font("微软雅黑", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left, y)
        };
        this.Controls.Add(lblStatus);

        y += 40;

        btnInstall = new Button
        {
            Text = "安装 UE4SS + PalSchema",
            Location = new Point(left, y),
            Size = new Size(180, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = ctrlFont
        };
        btnInstall.Click += async (s, e) =>
        {
            if (ue4 == null || !ue4.IsReady)
            {
                MessageBox.Show("UE4SS 模块不可用，请先选择有效的服务端路径。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (ue4.IsInstalled())
            {
                MessageBox.Show("UE4SS 已安装，无需重复安装。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus();
                return;
            }

            bool ok = await ue4.Install();
            UpdateStatus();

            if (!ok)
            {
                MessageBox.Show("安装失败，请查看日志窗口获取详细信息。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show(
                "安装成功，建议重启服务器以加载 UE4SS。是否立即重启？",
                "安装完成",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                if (mgr != null && mgr.IsRun)
                {
                    try
                    {
                        await mgr.ShutdownRst();
                        MessageBox.Show("服务器正在重启，请稍后查看状态。", "重启中", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"重启服务器失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("服务端未运行，请手动启动服务以加载 UE4SS。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("请记得手动重启服务器以使 UE4SS 生效。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        this.Controls.Add(btnInstall);

        btnUninstall = new Button
        {
            Text = "卸载 UE4SS",
            Location = new Point(left + 190, y),
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
            BackColor = Color.FromArgb(255, 235, 225),
            ForeColor = Color.FromArgb(180, 60, 0),
            Font = ctrlFont
        };

        btnUninstall.Click += (s, e) =>
        {
            if (ue4 == null || !ue4.IsReady)
            {
                MessageBox.Show("UE4SS 模块不可用。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ue4.IsInstalled())
            {
                MessageBox.Show("UE4SS 尚未安装，无需卸载。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 检查服务是否正在运行
            if (mgr != null && mgr.IsRun)
            {
                MessageBox.Show(
                    "检测到服务端正在运行。\n\n请先通过「服务器控制」→「强制停止」关闭服务器，然后再卸载 UE4SS。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return; // 不继续卸载
            }

            // 服务已停止，执行卸载
            ue4.Uninstall();
            MessageBox.Show("卸载完成。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateStatus();
        };
        this.Controls.Add(btnUninstall);

        // ---- 打开 UE4SS 目录按钮 ----
        btnOpenDir = new Button
        {
            Text = "📂 打开Win64目录",
            Location = new Point(left + 300, y),
            Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 200, 200) },
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = ctrlFont,
            Enabled = false
        };
        btnOpenDir.Click += (s, e) =>
        {
            if (ue4 == null || !ue4.IsReady || !ue4.IsInstalled())
            {
                MessageBox.Show("UE4SS 未安装或路径无效。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string dir = ue4.WinDir;
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
            {
                MessageBox.Show("目标目录不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开目录失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        this.Controls.Add(btnOpenDir);
    }
}