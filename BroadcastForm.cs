using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PalServerManager;

public class BroadcastForm : Form
{
    private SrvMgr mgr;
    private Config cfg;

    // ---- 控件 ----
    private Label lblMsg;
    private TextBox txtMessage;           // 多行消息输入
    private CheckBox chkSchedule;
    private DateTimePicker dtpSendTime;
    private Label lblInterval;
    private NumericUpDown numInterval;     // 循环间隔(秒)
    private CheckBox chkLoop;              // 无限循环
    private Button btnSendNow;
    private Button btnScheduleSend;
    private Button btnStartLoop;
    private Button btnStopLoop;
    private Button btnClose;

    // ---- 循环状态 ----
    private Timer loopTimer;
    private List<string> messages;
    private int currentIndex;
    private bool isLooping;

    public BroadcastForm(SrvMgr manager)
    {
        mgr = manager;
        cfg = mgr.Config;
        InitializeComponent();
        this.Font = new Font("微软雅黑", 9F);
        this.AutoScaleMode = AutoScaleMode.Font;

        // 加载保存的消息
        if (cfg.BroadcastMessages != null && cfg.BroadcastMessages.Count > 0)
            txtMessage.Text = string.Join(Environment.NewLine, cfg.BroadcastMessages);
        else
            txtMessage.Text = "第一条消息\n第二条消息\n第三条消息";

        // 加载循环设置
        chkLoop.Checked = cfg.BroadcastLoopInfinite;
        numInterval.Value = cfg.BroadcastInterval > 0 ? cfg.BroadcastInterval : 60;

        loopTimer = new Timer();
        loopTimer.Tick += LoopTimer_Tick;
        isLooping = false;
    }

    private void InitializeComponent()
    {
        this.Text = "广播消息";
        this.Size = new Size(600, 340);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 244, 248);

        Font lblFont = new Font("微软雅黑", 9F, FontStyle.Bold);
        Font ctrlFont = new Font("微软雅黑", 9F);

        int left = 20;
        int y = 20;
        int labelWidth = 80;
        int ctrlWidth = 180;

        // ---- 消息输入（多行） ----
        lblMsg = new Label
        {
            Text = "消息内容:",
            AutoSize = true,
            Font = lblFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left, y + 4)
        };
        this.Controls.Add(lblMsg);

        txtMessage = new TextBox
        {
            Location = new Point(left + labelWidth, y),
            Size = new Size(400, 80),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = ctrlFont,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90),
            Text = "第一条消息\n第二条消息\n第三条消息"
        };
        this.Controls.Add(txtMessage);

        y += 90;

        // ---- 定时发送区域 ----
        chkSchedule = new CheckBox
        {
            Text = "定时发送",
            AutoSize = true,
            Font = ctrlFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left, y + 3)
        };
        chkSchedule.CheckedChanged += (s, e) => dtpSendTime.Enabled = chkSchedule.Checked;
        this.Controls.Add(chkSchedule);

        dtpSendTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Location = new Point(left + 100, y),
            Size = new Size(160, 23),
            Font = ctrlFont,
            Enabled = false,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90),
            Value = DateTime.Now.AddMinutes(5)
        };
        this.Controls.Add(dtpSendTime);

        y += 40;

        // ---- 循环发送区域 ----
        Label lblLoop = new Label
        {
            Text = "循环发送:",
            AutoSize = true,
            Font = lblFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left, y + 4)
        };
        this.Controls.Add(lblLoop);

        chkLoop = new CheckBox
        {
            Text = "无限循环",
            AutoSize = true,
            Font = ctrlFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left + 80, y + 2)
        };
        this.Controls.Add(chkLoop);

        lblInterval = new Label
        {
            Text = "间隔(秒):",
            AutoSize = true,
            Font = ctrlFont,
            ForeColor = Color.FromArgb(40, 60, 90),
            Location = new Point(left + 180, y + 4)
        };
        this.Controls.Add(lblInterval);

        numInterval = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 3600,
            Value = 60,
            Location = new Point(left + 250, y),
            Size = new Size(60, 23),
            Font = ctrlFont,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 60, 90)
        };
        this.Controls.Add(numInterval);

        btnStartLoop = new Button
        {
            Text = "开始循环",
            Location = new Point(left + 330, y - 2),
            Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = ctrlFont
        };
        btnStartLoop.Click += BtnStartLoop_Click;
        this.Controls.Add(btnStartLoop);

        btnStopLoop = new Button
        {
            Text = "停止循环",
            Location = new Point(left + 430, y - 2),
            Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
            BackColor = Color.FromArgb(255, 235, 225),
            ForeColor = Color.FromArgb(180, 60, 0),
            Font = ctrlFont,
            Enabled = false
        };
        btnStopLoop.Click += BtnStopLoop_Click;
        this.Controls.Add(btnStopLoop);

        y += 45;

        // ---- 按钮行 ----
        int btnW = 100, btnH = 30;
        btnSendNow = new Button
        {
            Text = "立即发送",
            Location = new Point(left, y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
            BackColor = Color.FromArgb(225, 240, 255),
            ForeColor = Color.FromArgb(0, 80, 180),
            Font = ctrlFont
        };
        btnSendNow.Click += BtnSendNow_Click;
        this.Controls.Add(btnSendNow);

        btnScheduleSend = new Button
        {
            Text = "定时发送",
            Location = new Point(left + btnW + 10, y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 180, 0) },
            BackColor = Color.FromArgb(255, 245, 225),
            ForeColor = Color.FromArgb(180, 100, 0),
            Font = ctrlFont
        };
        btnScheduleSend.Click += BtnScheduleSend_Click;
        this.Controls.Add(btnScheduleSend);

        btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(left + 2 * (btnW + 10), y),
            Size = new Size(btnW, btnH),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
            BackColor = Color.FromArgb(240, 240, 240),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = ctrlFont
        };
        btnClose.Click += (s, e) => this.Close();
        this.Controls.Add(btnClose);

        // ---- 提示标签 ----
        Label lblHint = new Label
        {
            Text = "提示：每行一条消息，循环时按顺序轮流发送",
            AutoSize = true,
            Font = new Font("微软雅黑", 8F),
            ForeColor = Color.Gray,
            Location = new Point(left, y + 40)
        };
        this.Controls.Add(lblHint);
    }

    // ---- 立即发送 ----
    private async void BtnSendNow_Click(object sender, EventArgs e)
    {
        string msg = txtMessage.Text.Trim();
        if (string.IsNullOrEmpty(msg))
        {
            MessageBox.Show("请输入消息内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = msg });
        bool ok = await mgr.SendApi("announce", json);
        MessageBox.Show(ok ? "广播发送成功。" : "广播发送失败。", ok ? "成功" : "失败", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        if (ok) this.Close();
    }

    // ---- 定时发送（单次） ----
    private async void BtnScheduleSend_Click(object sender, EventArgs e)
    {
        if (!chkSchedule.Checked)
        {
            MessageBox.Show("请勾选“定时发送”并设置时间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string msg = txtMessage.Text.Trim();
        if (string.IsNullOrEmpty(msg))
        {
            MessageBox.Show("请输入消息内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DateTime target = dtpSendTime.Value;
        if (target <= DateTime.Now)
        {
            MessageBox.Show("发送时间必须晚于当前时间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        TimeSpan delay = target - DateTime.Now;
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = msg });
            await mgr.SendApi("announce", json);
        });
        MessageBox.Show($"广播已安排在 {target:HH:mm:ss} 发送。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.Close();
    }

    // ---- 开始循环 ----
    private void BtnStartLoop_Click(object sender, EventArgs e)
    {
        if (isLooping) return;

        messages = txtMessage.Lines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        if (messages.Count == 0)
        {
            MessageBox.Show("请在消息框中输入至少一行消息。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int interval = (int)numInterval.Value;
        if (interval < 1)
        {
            MessageBox.Show("间隔时间必须大于 0。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        currentIndex = 0;
        isLooping = true;
        loopTimer.Interval = interval * 1000;
        loopTimer.Start();

        btnStartLoop.Enabled = false;
        btnStopLoop.Enabled = true;
        chkSchedule.Enabled = false;
        dtpSendTime.Enabled = false;
        btnSendNow.Enabled = false;
        btnScheduleSend.Enabled = false;

        // 立即发送第一条
        SendCurrentMessage();
    }

    // ---- 停止循环 ----
    private void BtnStopLoop_Click(object sender, EventArgs e)
    {
        if (!isLooping) return;
        loopTimer.Stop();
        isLooping = false;
        btnStartLoop.Enabled = true;
        btnStopLoop.Enabled = false;
        chkSchedule.Enabled = true;
        dtpSendTime.Enabled = chkSchedule.Checked;
        btnSendNow.Enabled = true;
        btnScheduleSend.Enabled = true;
        MessageBox.Show("循环已停止。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ---- 循环定时器 Tick ----
    private async void LoopTimer_Tick(object sender, EventArgs e)
    {
        // 发送下一条
        if (messages.Count == 0) return;

        currentIndex++;
        if (currentIndex >= messages.Count)
        {
            if (chkLoop.Checked)
                currentIndex = 0;
            else
            {
                // 不循环，停止
                loopTimer.Stop();
                isLooping = false;
                btnStartLoop.Enabled = true;
                btnStopLoop.Enabled = false;
                chkSchedule.Enabled = true;
                dtpSendTime.Enabled = chkSchedule.Checked;
                btnSendNow.Enabled = true;
                btnScheduleSend.Enabled = true;
                MessageBox.Show("所有消息已发送完毕。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }
        SendCurrentMessage();
    }

    private async void SendCurrentMessage()
    {
        if (currentIndex < 0 || currentIndex >= messages.Count) return;
        string msg = messages[currentIndex];
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = msg });
        bool ok = await mgr.SendApi("announce", json);
        // 修复：使用 mgr.Log 输出日志
        mgr.Log($"[广播] 发送第 {currentIndex + 1}/{messages.Count} 条: \"{msg}\" {(ok ? "成功" : "失败")}");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // 保存消息列表
        var lines = txtMessage.Lines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();
        if (lines.Count > 0)
        {
            cfg.BroadcastMessages = lines;
        }

        // 保存循环设置
        cfg.BroadcastLoopInfinite = chkLoop.Checked;
        cfg.BroadcastInterval = (int)numInterval.Value;
        cfg.Save();

        loopTimer?.Stop();
        loopTimer?.Dispose();
        base.OnFormClosed(e);
    }
}