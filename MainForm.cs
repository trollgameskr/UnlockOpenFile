using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;

namespace UnlockOpenFile
{
    public class MainForm : Form
    {
        private ListView _fileListView = null!;
        private TextBox _logTextBox = null!;
        private Button _settingsButton = null!;
        private Button _closeAllButton = null!;
        private NotifyIcon? _notifyIcon;
        private readonly Dictionary<string, FileManager> _fileManagers = new();
        private System.Threading.Timer? _closeTimer;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "UnlockOpenFile - 파일 관리";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += OnFormClosing;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // File list view
            var fileGroup = new GroupBox
            {
                Text = "열린 파일 목록",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _fileListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _fileListView.Columns.Add("파일명", 200);
            _fileListView.Columns.Add("경로", 400);
            _fileListView.Columns.Add("상태", 150);

            fileGroup.Controls.Add(_fileListView);

            // Log textbox
            var logGroup = new GroupBox
            {
                Text = "로그",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _logTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            logGroup.Controls.Add(_logTextBox);

            // Bottom buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            _closeAllButton = new Button
            {
                Text = "모두 닫기",
                Width = 100,
                Height = 35,
                Margin = new Padding(5, 0, 0, 0)
            };
            _closeAllButton.Click += OnCloseAllClick;

            _settingsButton = new Button
            {
                Text = "설정",
                Width = 100,
                Height = 35,
                Margin = new Padding(5, 0, 0, 0)
            };
            _settingsButton.Click += OnSettingsClick;

            buttonPanel.Controls.Add(_closeAllButton);
            buttonPanel.Controls.Add(_settingsButton);

            mainPanel.Controls.Add(fileGroup, 0, 0);
            mainPanel.Controls.Add(logGroup, 0, 1);
            mainPanel.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainPanel);

            // System tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "UnlockOpenFile"
            };
            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            };

            // Context menu for tray icon
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            });
            contextMenu.Items.Add("설정", null, (s, e) => OnSettingsClick(s, e));
            contextMenu.Items.Add("종료", null, (s, e) => this.Close());
            _notifyIcon.ContextMenuStrip = contextMenu;

            AddLog("UnlockOpenFile가 시작되었습니다.");
        }

        public void OpenFile(string filePath)
        {
            try
            {
                if (_fileManagers.ContainsKey(filePath))
                {
                    AddLog($"파일이 이미 열려 있습니다: {filePath}");
                    UpdateFileStatus(filePath, "이미 열림");
                    return;
                }

                var fileManager = new FileManager(filePath);
                _fileManagers[filePath] = fileManager;

                fileManager.StatusChanged += (s, status) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() => UpdateFileStatus(filePath, status));
                    }
                    else
                    {
                        UpdateFileStatus(filePath, status);
                    }
                };

                fileManager.FileModified += (s, e) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            AddLog($"파일 수정됨: {System.IO.Path.GetFileName(filePath)}");
                            _notifyIcon?.ShowBalloonTip(2000, "파일 수정됨",
                                $"{System.IO.Path.GetFileName(filePath)}이(가) 수정되어 원본에 저장 중입니다.",
                                ToolTipIcon.Info);
                        });
                    }
                };

                fileManager.FileSaved += (s, e) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() =>
                        {
                            AddLog($"저장 완료: {System.IO.Path.GetFileName(filePath)} - 변경 사항이 원본에 저장되었습니다.");
                            _notifyIcon?.ShowBalloonTip(2000, "저장 완료",
                                $"{System.IO.Path.GetFileName(filePath)}의 변경 사항이 원본 파일에 저장되었습니다.",
                                ToolTipIcon.Info);
                        });
                    }
                };

                fileManager.ProcessExited += (s, e) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(() => RemoveFile(filePath));
                    }
                    else
                    {
                        RemoveFile(filePath);
                    }
                };

                AddFileToList(filePath);
                _ = fileManager.OpenFileAsync();

                AddLog($"파일 열기: {filePath}");
            }
            catch (Exception ex)
            {
                AddLog($"파일 열기 오류: {ex.Message}");
                MessageBox.Show($"파일을 열 수 없습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddFileToList(string filePath)
        {
            var item = new ListViewItem(System.IO.Path.GetFileName(filePath));
            item.SubItems.Add(filePath);
            item.SubItems.Add("열기 중...");
            item.Tag = filePath;
            _fileListView.Items.Add(item);
        }

        private void UpdateFileStatus(string filePath, string status)
        {
            var item = _fileListView.Items.Cast<ListViewItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == filePath);

            if (item != null)
            {
                item.SubItems[2].Text = status;
            }

            AddLog($"[{System.IO.Path.GetFileName(filePath)}] {status}");
        }

        private void RemoveFile(string filePath)
        {
            try
            {
                if (_fileManagers.ContainsKey(filePath))
                {
                    _fileManagers[filePath].Cleanup();
                    _fileManagers.Remove(filePath);
                }

                var item = _fileListView.Items.Cast<ListViewItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == filePath);

                if (item != null)
                {
                    _fileListView.Items.Remove(item);
                }

                AddLog($"파일 닫힘: {System.IO.Path.GetFileName(filePath)}");

                // If no more files are open, close the application after 5 seconds
                if (_fileManagers.Count == 0)
                {
                    AddLog("모든 파일이 닫혔습니다. 5초 후 프로그램을 종료합니다.");
                    
                    // Cancel any existing close timer
                    _closeTimer?.Dispose();
                    
                    // Create a countdown timer that updates every second
                    int countdown = 5;
                    _closeTimer = new System.Threading.Timer(_ =>
                    {
                        if (this.IsDisposed) return;
                        
                        if (this.InvokeRequired)
                        {
                            this.Invoke(() =>
                            {
                                countdown--;
                                if (countdown > 0)
                                {
                                    AddLog($"{countdown}초 후 종료...");
                                }
                                else
                                {
                                    AddLog("프로그램을 종료합니다.");
                                    _closeTimer?.Dispose();
                                    _closeTimer = null;
                                    this.Close();
                                }
                            });
                        }
                    }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex)
            {
                AddLog($"파일 제거 오류: {ex.Message}");
            }
        }

        private void OnCloseAllClick(object? sender, EventArgs e)
        {
            var filePaths = _fileManagers.Keys.ToList();
            foreach (var filePath in filePaths)
            {
                RemoveFile(filePath);
            }
        }

        private void OnSettingsClick(object? sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel any pending close timer
            _closeTimer?.Dispose();
            _closeTimer = null;
            
            // Clean up all file managers
            foreach (var fileManager in _fileManagers.Values)
            {
                fileManager.Cleanup();
            }
            _fileManagers.Clear();

            _notifyIcon?.Dispose();
        }

        private void AddLog(string message)
        {
            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.Invoke(() => AddLog(message));
                return;
            }

            _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            _logTextBox.SelectionStart = _logTextBox.Text.Length;
            _logTextBox.ScrollToCaret();
        }
    }
}
