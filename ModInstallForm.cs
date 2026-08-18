using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PalServerManager
{
    public class ModInstallForm : Form
    {
        private ModManager modMgr;
        private string modName;
        private DataGridView dgvFiles;
        private ComboBox cmbPreset;
        private Button btnApplyPreset, btnSetPath, btnOK, btnCancel;
        private string palRoot;
        private Panel bottomPanel;
        private Label hint;

        public ModInstallForm(ModManager manager, string name)
        {
            modMgr = manager;
            modName = name;
            palRoot = modMgr.GetPalRoot();
            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            LoadFiles();
        }

        private void InitializeComponent()
        {
            this.Text = $"安装设置 - {modName}";
            this.Size = new Size(850, 520);
            this.MinimumSize = new Size(750, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(240, 244, 248);

            // ---- 文件表格（填充剩余空间） ----
            dgvFiles = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Font = new Font("微软雅黑", 9F),
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                EditMode = DataGridViewEditMode.EditOnKeystroke
            };

            // 列0：复选框
            var chkCol = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "自选",
                Width = 40,
                ReadOnly = false,
                TrueValue = true,
                FalseValue = false
            };
            dgvFiles.Columns.Add(chkCol);
            dgvFiles.Columns.Add("File", "文件（相对路径）");
            dgvFiles.Columns.Add("Target", "目标路径（相对）");
            dgvFiles.Columns[1].Width = 250;
            dgvFiles.Columns[2].Width = 400;

            dgvFiles.CellFormatting += DgvFiles_CellFormatting;
            dgvFiles.CellToolTipTextNeeded += DgvFiles_CellToolTipTextNeeded;

            // ---- 底部按钮面板（固定在底部） ----
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,  // 容纳两行控件
                BackColor = Color.Transparent,
                Padding = new Padding(12, 6, 12, 6)
            };

            // 使用 TableLayoutPanel 来整齐排列两行
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // 标签
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // ComboBox
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140)); // 应用按钮
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // 设置路径按钮
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210)); // 安装/取消
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

            // 第一行：标签 + ComboBox + 应用至所有文件 + 设置路径 + （右对齐按钮）
            // 标签
            table.Controls.Add(new Label
            {
                Text = "快速应用预设:",
                AutoSize = true,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 90),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 0, 0)
            }, 0, 0);

            cmbPreset = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9F),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 4, 10, 0)
            };
            cmbPreset.Items.AddRange(modMgr.GetPresetKeys().ToArray());
            if (cmbPreset.Items.Count > 0) cmbPreset.SelectedIndex = 0;
            table.Controls.Add(cmbPreset, 1, 0);

            btnApplyPreset = new Button
            {
                Text = "应用至所有文件",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
                BackColor = Color.FromArgb(225, 240, 255),
                ForeColor = Color.FromArgb(0, 80, 180),
                Font = new Font("微软雅黑", 9F),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 2, 10, 0),
                Height = 30,
                Width = 130
            };
            btnApplyPreset.Click += BtnApplyPreset_Click;
            table.Controls.Add(btnApplyPreset, 2, 0);

            btnSetPath = new Button
            {
                Text = "自选路径",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 200, 100) },
                BackColor = Color.FromArgb(245, 245, 230),
                ForeColor = Color.FromArgb(120, 100, 0),
                Font = new Font("微软雅黑", 9F),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 2, 10, 0),
                Height = 30,
                Width = 100
            };
            btnSetPath.Click += BtnSetPath_Click;
            table.Controls.Add(btnSetPath, 3, 0);

            // 右对齐的安装/取消（放在第一行第四列）
            var rightFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 2, 0, 0),
                Height = 30,
                Width = 210
            };
            btnOK = new Button
            {
                Text = "安装",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 100) },
                BackColor = Color.FromArgb(230, 245, 230),
                ForeColor = Color.FromArgb(0, 120, 0),
                Font = new Font("微软雅黑", 9F),
                Width = 100,
                Height = 30,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnOK.Click += BtnOK_Click;
            rightFlow.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "取消",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F),
                Width = 100,
                Height = 30
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            rightFlow.Controls.Add(btnCancel);

            table.Controls.Add(rightFlow, 4, 0);

            // 第二行：提示标签（占满所有列）
            hint = new Label
            {
                Text = "提示：勾选文件后点击「设置路径」统一指定目标目录；直接编辑路径也支持（相对路径，悬停查看完整）。",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            table.SetColumnSpan(hint, 5);
            table.Controls.Add(hint, 0, 1);

            bottomPanel.Controls.Add(table);
            this.Controls.Add(dgvFiles);
            this.Controls.Add(bottomPanel);
        }

        private void LoadFiles()
        {
            var files = modMgr.GetModFiles(modName);
            var existingMap = modMgr.GetInstallMap(modName);

            dgvFiles.Rows.Clear();
            foreach (string relPath in files)
            {
                string target = existingMap.TryGetValue(relPath, out string fullPath) ? fullPath : "";
                dgvFiles.Rows.Add(false, relPath, target);
            }
            dgvFiles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void DgvFiles_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 2 && e.Value != null && !string.IsNullOrEmpty(e.Value.ToString()))
            {
                string full = e.Value.ToString();
                if (!string.IsNullOrEmpty(palRoot) && full.StartsWith(palRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string relative = full.Substring(palRoot.Length).TrimStart('\\', '/');
                    if (string.IsNullOrEmpty(relative))
                        relative = ".";
                    e.Value = relative;
                    e.FormattingApplied = true;
                }
            }
        }

        private void DgvFiles_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.ColumnIndex == 2 && e.RowIndex >= 0)
            {
                var cell = dgvFiles.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    e.ToolTipText = cell.Value.ToString();
                }
            }
        }

        private void BtnApplyPreset_Click(object sender, EventArgs e)
        {
            if (cmbPreset.SelectedItem == null) return;
            string presetKey = cmbPreset.SelectedItem.ToString();

            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if (row.IsNewRow) continue;
                string relPath = row.Cells[1].Value?.ToString();
                if (string.IsNullOrEmpty(relPath)) continue;

                string fullTarget = modMgr.GetPresetFullPath(presetKey, modName, relPath);
                row.Cells[2].Value = fullTarget ?? "";
            }
            dgvFiles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void BtnSetPath_Click(object sender, EventArgs e)
        {
            var selectedRows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if (row.IsNewRow) continue;
                var chkCell = row.Cells[0] as DataGridViewCheckBoxCell;
                if (chkCell != null && chkCell.Value != null && (bool)chkCell.Value == true)
                {
                    selectedRows.Add(row);
                }
            }

            if (selectedRows.Count == 0)
            {
                MessageBox.Show("请先勾选要设置路径的文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = $"选择目标目录（将自动保留文件的相对路径结构，共 {selectedRows.Count} 个文件）";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string selectedDir = fbd.SelectedPath;
                    foreach (var row in selectedRows)
                    {
                        string relPath = row.Cells[1].Value?.ToString();
                        if (string.IsNullOrEmpty(relPath)) continue;
                        string fullTarget = System.IO.Path.Combine(selectedDir, relPath);
                        row.Cells[2].Value = fullTarget;
                    }
                    dgvFiles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            var fileMap = new Dictionary<string, string>();
            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if (row.IsNewRow) continue;
                string relPath = row.Cells[1].Value?.ToString();
                string target = row.Cells[2].Value?.ToString();
                if (string.IsNullOrEmpty(relPath))
                    continue;

                if (!string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(palRoot))
                {
                    if (!System.IO.Path.IsPathRooted(target) && !target.StartsWith("\\") && !target.StartsWith("/"))
                    {
                        target = System.IO.Path.Combine(palRoot, target);
                    }
                }

                if (string.IsNullOrEmpty(target))
                {
                    MessageBox.Show($"文件 {relPath} 的目标路径不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                fileMap[relPath] = target;
            }

            if (modMgr.InstallMod(modName, fileMap))
            {
                MessageBox.Show("安装成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}