using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace PalServerManager
{
    public class InfoForm : Form
    {
        private TextBox txtContent;
        private Button btnClose, btnCopy;

        // 信息字段翻译
        private static readonly Dictionary<string, string> InfoTranslation = new()
        {
            ["version"] = "服务端版本",
            ["servername"] = "服务器名称",
            ["description"] = "服务器描述",
            ["word1guid"] = "世界 GUID"
        };

        // 指标字段翻译
        private static readonly Dictionary<string, string> MetricsTranslation = new()
        {
            ["currentplayernum"] = "当前玩家数",
            ["serverfps"] = "服务器 FPS",
            ["serverfpsaverage"] = "平均 FPS",
            ["serverframetime"] = "帧时间 (ms)",
            ["days"] = "游戏天数",
            ["maxplayernum"] = "最大玩家数",
            ["basecampnum"] = "据点数量",
            ["uptime"] = "运行时间 (秒)"
        };

        public InfoForm(string title, string json)
        {
            this.Text = title;
            this.Size = new Size(500, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(400, 300);
            this.BackColor = Color.FromArgb(240, 244, 248);

            txtContent = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(460, 310),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                ReadOnly = true
            };

            // 解析 JSON 并翻译
            string displayText = TranslateJson(json, title);
            txtContent.Text = displayText;
            this.Controls.Add(txtContent);

            int y = txtContent.Bottom + 12;
            btnCopy = new Button
            {
                Text = "复制内容",
                Location = new Point(12, y),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
                BackColor = Color.FromArgb(225, 240, 255),
                ForeColor = Color.FromArgb(0, 80, 180),
                Font = new Font("微软雅黑", 9F)
            };
            btnCopy.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtContent.Text))
                {
                    Clipboard.SetText(txtContent.Text);
                    MessageBox.Show("已复制到剪贴板。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            this.Controls.Add(btnCopy);

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(120, y),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private string TranslateJson(string json, string title)
        {
            try
            {
                var obj = JObject.Parse(json);
                var dict = title.Contains("信息") ? InfoTranslation : MetricsTranslation;
                var lines = new List<string>();
                foreach (var prop in obj.Properties())
                {
                    string key = prop.Name;
                    string value = prop.Value.ToString();
                    string cnKey = dict.ContainsKey(key) ? dict[key] : key; // 无翻译则显示英文
                    lines.Add($"{cnKey}：{value}");
                }
                return string.Join(Environment.NewLine, lines);
            }
            catch
            {
                // 解析失败，返回原始 JSON（格式化）
                try
                {
                    var parsed = JToken.Parse(json);
                    return parsed.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    return json;
                }
            }
        }
    }
}