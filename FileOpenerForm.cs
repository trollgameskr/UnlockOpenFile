using System;
using System.Drawing;
using System.Windows.Forms;

namespace UnlockOpenFile
{
    public class FileOpenerForm : Form
    {
        private readonly FileManager _fileManager;
        private TextBox _statusTextBox = null!;
        private Button _closeButton = null!;
        private Label _fileLabel = null!;
        private NotifyIcon? _notifyIcon;

        public FileOpenerForm(string filePath)
        {
            try
            {
                _fileManager = new FileManager(filePath);
                InitializeComponents();
                InitializeFileManager();
                _ = OpenFileAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 초기화 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeComponents()
        {
            this.Text = "UnlockOpenFile";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = IconHelper.LoadApplicationIcon();
            this.FormClosing += OnFormClosing;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            _fileLabel = new Label
            {
                Text = "파일 처리 중...",
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };

            _statusTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            _closeButton = new Button
            {
                Text = "닫기",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 30
            };
            _closeButton.Click += (s, e) => this.Close();

            mainPanel.Controls.Add(_fileLabel, 0, 0);
            mainPanel.Controls.Add(_statusTextBox, 0, 1);
            mainPanel.Controls.Add(_closeButton, 0, 2);

            this.Controls.Add(mainPanel);

            // System tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.LoadApplicationIcon(),
                Visible = true,
                Text = "UnlockOpenFile"
            };
            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };

            // Context menu for tray icon
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            });
            contextMenu.Items.Add("종료", null, (s, e) => this.Close());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializeFileManager()
        {
            _fileManager.StatusChanged += (s, status) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(() => UpdateStatus(status));
                }
                else
                {
                    UpdateStatus(status);
                }
            };

            _fileManager.FileModified += (s, e) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(() => _notifyIcon?.ShowBalloonTip(2000, "파일 수정됨", 
                        "파일이 수정되어 원본에 저장 중입니다.", ToolTipIcon.Info));
                }
            };

            _fileManager.FileSaved += (s, e) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(() => _notifyIcon?.ShowBalloonTip(2000, "저장 완료", 
                        "변경 사항이 원본 파일에 저장되었습니다.", ToolTipIcon.Info));
                }
            };
            
            _fileManager.ProcessExited += (s, e) =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(() => this.Close());
                }
                else
                {
                    this.Close();
                }
            };
        }

        private async System.Threading.Tasks.Task OpenFileAsync()
        {
            bool success = await _fileManager.OpenFileAsync();
            if (!success)
            {
                MessageBox.Show("파일을 열 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void UpdateStatus(string status)
        {
            _statusTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {status}\r\n");
            _statusTextBox.SelectionStart = _statusTextBox.Text.Length;
            _statusTextBox.ScrollToCaret();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _fileManager.Cleanup();
            _notifyIcon?.Dispose();
        }
    }
}
