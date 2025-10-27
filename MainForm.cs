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
        private ListView _recentFilesListView = null!;
        private TextBox _logTextBox = null!;
        private Button _settingsButton = null!;
        private Button _closeAllButton = null!;
        private NotifyIcon? _notifyIcon;
        private readonly Dictionary<string, FileManager> _fileManagers = new();
        private System.Threading.Timer? _closeTimer;

        public MainForm()
        {
            InitializeComponents();
            _ = CheckForUpdatesOnStartup();
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
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
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

            // Recent files list view
            var recentFilesGroup = new GroupBox
            {
                Text = "최근 열었던 파일",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _recentFilesListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _recentFilesListView.Columns.Add("파일명", 200);
            _recentFilesListView.Columns.Add("경로", 400);
            _recentFilesListView.DoubleClick += OnRecentFileDoubleClick;

            recentFilesGroup.Controls.Add(_recentFilesListView);

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
            mainPanel.Controls.Add(recentFilesGroup, 0, 1);
            mainPanel.Controls.Add(logGroup, 0, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 3);

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

            // Load recent files
            LoadRecentFiles();

            AddLog("UnlockOpenFile가 시작되었습니다.");
        }

        public void OpenFile(string filePath)
        {
            try
            {
                // Check if this file belongs to a group
                var groupName = FileGroupManager.GetFileGroup(filePath);
                if (!string.IsNullOrEmpty(groupName))
                {
                    var groupFiles = FileGroupManager.GetGroupFiles(groupName);
                    AddLog($"파일이 '{groupName}' 그룹에 속해 있습니다. 그룹의 모든 파일을 엽니다.");
                    
                    // Open all files in the group
                    foreach (var groupFilePath in groupFiles)
                    {
                        if (System.IO.File.Exists(groupFilePath))
                        {
                            OpenSingleFile(groupFilePath);
                        }
                        else
                        {
                            AddLog($"파일을 찾을 수 없습니다: {groupFilePath}");
                        }
                    }
                }
                else
                {
                    // File is not in a group, open it normally
                    OpenSingleFile(filePath);
                }
            }
            catch (Exception ex)
            {
                AddLog($"파일 열기 오류: {ex.Message}");
                MessageBox.Show($"파일을 열 수 없습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSingleFile(string filePath)
        {
            try
            {
                if (_fileManagers.ContainsKey(filePath))
                {
                    // Check if the file is actually still in use
                    var existingManager = _fileManagers[filePath];
                    if (!existingManager.IsFileStillInUse())
                    {
                        // File is no longer in use, clean it up and allow reopening
                        AddLog($"기존 파일이 더 이상 사용 중이 아닙니다. 정리 중...: {filePath}");
                        RemoveFile(filePath);
                        // Continue to open the file below
                    }
                    else
                    {
                        AddLog($"파일이 이미 열려 있습니다: {filePath}");
                        UpdateFileStatus(filePath, "이미 열림");
                        
                        // Show and bring the main form to front
                        this.Show();
                        this.WindowState = FormWindowState.Normal;
                        this.BringToFront();
                        return;
                    }
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

                // Add to recent files
                RecentFilesManager.AddRecentFile(filePath);
                LoadRecentFiles();

                AddLog($"파일 열기: {filePath}");
            }
            catch (Exception ex)
            {
                AddLog($"단일 파일 열기 오류: {ex.Message}");
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

                // If no more files are open, close the application immediately
                if (_fileManagers.Count == 0)
                {
                    AddLog("모든 파일이 닫혔습니다. 프로그램을 종료합니다.");
                    
                    // Cancel any existing close timer
                    _closeTimer?.Dispose();
                    _closeTimer = null;
                    
                    this.Close();
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

        private void LoadRecentFiles()
        {
            if (_recentFilesListView.InvokeRequired)
            {
                _recentFilesListView.Invoke(() => LoadRecentFiles());
                return;
            }

            _recentFilesListView.Items.Clear();
            var recentFiles = RecentFilesManager.GetRecentFiles();
            
            foreach (var filePath in recentFiles)
            {
                var item = new ListViewItem(System.IO.Path.GetFileName(filePath));
                item.SubItems.Add(filePath);
                item.Tag = filePath;
                _recentFilesListView.Items.Add(item);
            }
        }

        private void OnRecentFileDoubleClick(object? sender, EventArgs e)
        {
            if (_recentFilesListView.SelectedItems.Count > 0)
            {
                var selectedItem = _recentFilesListView.SelectedItems[0];
                var filePath = selectedItem.Tag?.ToString();
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        OpenFile(filePath);
                    }
                    else
                    {
                        MessageBox.Show($"파일을 찾을 수 없습니다: {filePath}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AddLog($"파일을 찾을 수 없습니다: {filePath}");
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task CheckForUpdatesOnStartup()
        {
            try
            {
                // Wait a bit before checking to avoid blocking UI initialization
                await System.Threading.Tasks.Task.Delay(2000);

                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();

                if (updateInfo != null && updateInfo.IsUpdateAvailable)
                {
                    AddLog($"새 버전을 사용할 수 있습니다: v{updateInfo.LatestVersion}");
                    
                    // Show balloon tip notification
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.ShowBalloonTip(5000,
                            "업데이트 알림",
                            $"새로운 버전 v{updateInfo.LatestVersion}이(가) 출시되었습니다. 설정에서 업데이트를 확인하세요.",
                            ToolTipIcon.Info);
                    }
                }
            }
            catch
            {
                // Fail silently on startup - don't interrupt user experience
            }
        }
    }
}
