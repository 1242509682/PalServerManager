using System;
using System.Drawing;
using System.Windows.Forms;

namespace PalServerManager
{
    public class ModForm : Form
    {
        private ModManager modMgr;
        private DataGridView dgvMods;
        private Button btnRefresh, btnInstall, btnUninstall, btnOpenDir, btnOpenMyModFolder, btnClose;
        private Panel bottomPanel;

        public ModForm(ModManager manager)
        {
            modMgr = manager;
            InitializeComponent();
            this.Font = new Font("微软雅黑", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "MOD 管理";
            this.Size = new Size(780, 450);
            this.MinimumSize = new Size(700, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(240, 244, 248);

            dgvMods = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ForeColor = Color.FromArgb(30, 60, 90),
                Font = new Font("微软雅黑", 9F),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
            };
            dgvMods.Columns.Add("Name", "Mod名称");
            dgvMods.Columns.Add("Status", "状态");
            dgvMods.Columns.Add("FileCount", "文件数");
            dgvMods.CellDoubleClick += DgvMods_CellDoubleClick;

            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 10)
            };

            int btnW = 90, btnH = 30;
            int left = 12;
            int y = 10;

            btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(left, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(100, 200, 200) },
                BackColor = Color.FromArgb(225, 245, 245),
                ForeColor = Color.FromArgb(0, 100, 120),
                Font = new Font("微软雅黑", 9F)
            };
            btnRefresh.Click += (s, e) => RefreshList();
            bottomPanel.Controls.Add(btnRefresh);

            btnInstall = new Button
            {
                Text = "安装",
                Location = new Point(left + btnW + 8, y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(0, 180, 255) },
                BackColor = Color.FromArgb(225, 240, 255),
                ForeColor = Color.FromArgb(0, 80, 180),
                Font = new Font("微软雅黑", 9F)
            };
            btnInstall.Click += BtnInstall_Click;
            bottomPanel.Controls.Add(btnInstall);

            btnUninstall = new Button
            {
                Text = "卸载",
                Location = new Point(left + 2 * (btnW + 8), y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(255, 150, 100) },
                BackColor = Color.FromArgb(255, 235, 225),
                ForeColor = Color.FromArgb(180, 60, 0),
                Font = new Font("微软雅黑", 9F)
            };
            btnUninstall.Click += BtnUninstall_Click;
            bottomPanel.Controls.Add(btnUninstall);

            btnOpenDir = new Button
            {
                Text = "进入Mod",
                Location = new Point(left + 3 * (btnW + 8), y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 200, 200) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };
            btnOpenDir.Click += BtnOpenDir_Click;
            bottomPanel.Controls.Add(btnOpenDir);

            btnOpenMyModFolder = new Button
            {
                Text = "📂",
                Location = new Point(left + 4 * (btnW + 8), y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 200, 200) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };
            btnOpenMyModFolder.Click += (s, e) =>
            {
                string path = modMgr.MyModFolder;
                if (System.IO.Directory.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", path);
                else
                    MessageBox.Show("MyMod目录不存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            bottomPanel.Controls.Add(btnOpenMyModFolder);

            btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(left + 5 * (btnW + 8), y),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(180, 180, 180) },
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("微软雅黑", 9F)
            };
            btnClose.Click += (s, e) => this.Close();
            bottomPanel.Controls.Add(btnClose);

            this.Controls.Add(dgvMods);
            this.Controls.Add(bottomPanel);
        }

        private void RefreshList()
        {
            dgvMods.Rows.Clear();
            var mods = modMgr.GetModFolders();
            foreach (string modName in mods)
            {
                bool installed = modMgr.IsInstalled(modName);
                int fileCount = modMgr.GetModFiles(modName).Count;
                string status = installed ? "已安装" : "未安装";
                dgvMods.Rows.Add(modName, status, fileCount);
            }
            dgvMods.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (dgvMods.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个Mod。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string modName = dgvMods.SelectedRows[0].Cells[0].Value.ToString();
            using var installForm = new ModInstallForm(modMgr, modName);
            if (installForm.ShowDialog() == DialogResult.OK)
                RefreshList();
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            if (dgvMods.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个Mod。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string modName = dgvMods.SelectedRows[0].Cells[0].Value.ToString();
            if (MessageBox.Show($"确认卸载 Mod “{modName}”？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                modMgr.UninstallMod(modName);
                RefreshList();
            }
        }

        private void BtnOpenDir_Click(object sender, EventArgs e)
        {
            if (dgvMods.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个Mod。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string modName = dgvMods.SelectedRows[0].Cells[0].Value.ToString();
            modMgr.OpenModFolder(modName);
        }

        private void DgvMods_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string modName = dgvMods.Rows[e.RowIndex].Cells[0].Value.ToString();
            using var installForm = new ModInstallForm(modMgr, modName);
            if (installForm.ShowDialog() == DialogResult.OK)
                RefreshList();
        }
    }
}