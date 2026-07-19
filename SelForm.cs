using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager;

public partial class SelForm : Form
{
    private List<Process> procs;
    public Process SelProc { get; private set; }

    public SelForm(List<Process> list)
    {
        InitComp();
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;
        procs = list;
        listBox1.Items.Clear();
        foreach (var p in procs)
            listBox1.Items.Add($"PID:{p.Id}  {p.ProcessName}  {p.StartTime}");
        if (listBox1.Items.Count > 0) listBox1.SelectedIndex = 0;
    }

    private void InitComp()
    {
        this.Text = "选择服务端进程";
        this.Size = new Size(540, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 244, 248);

        this.listBox1 = new ListBox
        {
            Dock = DockStyle.Top,
            Height = 300,
            Font = new Font("微软雅黑", 9F),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90)
        };
        this.listBox1.DoubleClick += (s, e) => btnOk.PerformClick();

        this.btnOk = new Button
        {
            Text = "选择",
            Location = new Point(140, 320),
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = new Font("微软雅黑", 9F)
        };
        btnOk.Click += BtnOk_Click;

        this.btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(280, 320),
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = new Font("微软雅黑", 9F)
        };
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.listBox1);
    }

    private void BtnOk_Click(object s, EventArgs e)
    {
        int idx = listBox1.SelectedIndex;
        if (idx >= 0 && idx < procs.Count) { SelProc = procs[idx]; this.DialogResult = DialogResult.OK; this.Close(); }
        else MessageBox.Show("请先选择一个进程。");
    }

    private ListBox listBox1;
    private Button btnOk, btnCancel;
}