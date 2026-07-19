using System;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager;

public class Ue4Form : Form
{
    private Ue4Mgr ue4;
    private Button btnInstall, btnUninstall;
    private Label lblStatus;

    public Ue4Form(Ue4Mgr manager)
    {
        ue4 = manager;
        InitializeComponent();
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (ue4 == null || !ue4.IsReady)
            lblStatus.Text = "UE4SS 不可用（请将程序放在服务端根目录）";
        else if (ue4.IsInstalled())
            lblStatus.Text = "UE4SS 已安装";
        else
            lblStatus.Text = "UE4SS 未安装";
    }

    private void InitializeComponent()
    {
        this.Text = "UE4SS 管理";
        this.Size = new Size(400, 170);
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
            if (ue4 == null || !ue4.IsReady) { MessageBox.Show("UE4SS 模块不可用。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            bool ok = await ue4.Install();
            MessageBox.Show(ok ? "安装成功。" : "安装失败，请查看日志。", ok ? "成功" : "失败", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            UpdateStatus();
        };
        this.Controls.Add(btnInstall);

        btnUninstall = new Button
        {
            Text = "卸载 UE4SS",
            Location = new Point(left + 190, y),
            Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
            BackColor = Color.FromArgb(255, 235, 225),
            ForeColor = Color.FromArgb(180, 60, 0),
            Font = ctrlFont
        };
        btnUninstall.Click += (s, e) =>
        {
            if (ue4 == null || !ue4.IsReady) { MessageBox.Show("UE4SS 模块不可用。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (MessageBox.Show("确认卸载 UE4SS？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ue4.Uninstall();
                MessageBox.Show("卸载完成。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus();
            }
        };
        this.Controls.Add(btnUninstall);
    }
}