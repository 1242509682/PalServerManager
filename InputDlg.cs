using System;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager;

public partial class InputDlg : Form
{
    public string User { get; private set; }
    public string Pass { get; private set; }

    private TextBox txtUser, txtPass;
    private Button btnOk, btnCancel;

    public InputDlg(string msg, string title)
    {
        this.Text = title;
        this.Size = new Size(300, 200);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;

        var lbl = new Label
        {
            Text = msg,
            Location = new Point(16, 16),
            AutoSize = true,
            Font = new Font("微软雅黑", 9F),
            ForeColor = Color.FromArgb(40, 60, 90)
        };

        txtUser = new TextBox
        {
            Location = new Point(16, 46),
            Width = 250,
            PlaceholderText = "Steam 用户名",
            Font = new Font("微软雅黑", 9F),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90)
        };
        txtPass = new TextBox
        {
            Location = new Point(16, 76),
            Width = 250,
            PlaceholderText = "Steam 密码",
            PasswordChar = '*',
            Font = new Font("微软雅黑", 9F),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90)
        };

        btnOk = new Button
        {
            Text = "确定",
            Location = new Point(60, 120),
            Width = 80,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = new Font("微软雅黑", 9F)
        };
        btnOk.Click += (s, e) => { User = txtUser.Text; Pass = txtPass.Text; this.DialogResult = DialogResult.OK; this.Close(); };

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(160, 120),
            Width = 80,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = new Font("微软雅黑", 9F)
        };
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        this.Controls.Add(lbl);
        this.Controls.Add(txtUser);
        this.Controls.Add(txtPass);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
    }
}