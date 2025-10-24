using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public class SettingsForm : Form
    {
        private CheckBox _startupCheckBox = null!;
        private Button _registerExcelButton = null!;
        private Button _registerCsvButton = null!;
        private Button _unregisterButton = null!;
        private Button _closeButton = null!;
        private Button _resetAllButton = null!;
        private Button _clearRecentFilesButton = null!;
        private TextBox _logTextBox = null!;
        private GroupBox _fileAssociationGroup = null!;
        private GroupBox _startupGroup = null!;
        private GroupBox _customApplicationGroup = null!;
        private ListView _applicationListView = null!;
        private Button _addApplicationButton = null!;
        private Button _removeApplicationButton = null!;
        private GroupBox _fileGroupsGroup = null!;
        private ListView _fileGroupsListView = null!;
        private Button _addGroupButton = null!;
        private Button _removeGroupButton = null!;
        private Button _manageGroupFilesButton = null!;
        private Button _renameGroupButton = null!;

        public SettingsForm()
        {
            InitializeComponents();
            LoadCurrentSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "UnlockOpenFile - 설정";
            this.Size = new Size(700, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            // Startup group
            _startupGroup = new GroupBox
            {
                Text = "시작 프로그램",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _startupCheckBox = new CheckBox
            {
                Text = "Windows 시작 시 자동 실행",
                Location = new Point(20, 30),
                AutoSize = true
            };
            _startupCheckBox.CheckedChanged += OnStartupCheckChanged;
            _startupGroup.Controls.Add(_startupCheckBox);

            var startupLabel = new Label
            {
                Text = "이 옵션을 선택하면 Windows 시작 시 프로그램이 백그라운드에서 실행됩니다.",
                Location = new Point(20, 60),
                Size = new Size(600, 20),
                ForeColor = Color.Gray
            };
            _startupGroup.Controls.Add(startupLabel);

            _clearRecentFilesButton = new Button
            {
                Text = "최근 파일 목록 지우기",
                Location = new Point(20, 85),
                Width = 180,
                Height = 25
            };
            _clearRecentFilesButton.Click += OnClearRecentFilesClick;
            _startupGroup.Controls.Add(_clearRecentFilesButton);

            // File association group
            _fileAssociationGroup = new GroupBox
            {
                Text = "파일 연결",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var associationLabel = new Label
            {
                Text = "특정 파일 형식을 이 프로그램으로 열도록 설정:",
                Location = new Point(20, 25),
                AutoSize = true
            };
            _fileAssociationGroup.Controls.Add(associationLabel);

            _registerExcelButton = new Button
            {
                Text = "Excel 파일 (.xlsx) 연결",
                Location = new Point(20, 50),
                Width = 180
            };
            _registerExcelButton.Click += (s, e) => RegisterFileAssociation(".xlsx", "Excel 파일");

            _registerCsvButton = new Button
            {
                Text = "CSV 파일 (.csv) 연결",
                Location = new Point(210, 50),
                Width = 180
            };
            _registerCsvButton.Click += (s, e) => RegisterFileAssociation(".csv", "CSV 파일");

            _unregisterButton = new Button
            {
                Text = "연결 해제",
                Location = new Point(400, 50),
                Width = 180
            };
            _unregisterButton.Click += OnUnregisterClick;

            _fileAssociationGroup.Controls.Add(_registerExcelButton);
            _fileAssociationGroup.Controls.Add(_registerCsvButton);
            _fileAssociationGroup.Controls.Add(_unregisterButton);

            // Custom application group
            _customApplicationGroup = new GroupBox
            {
                Text = "사용자 지정 응용 프로그램",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var customAppLabel = new Label
            {
                Text = "파일 확장자별로 사용할 응용 프로그램을 지정할 수 있습니다:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            _customApplicationGroup.Controls.Add(customAppLabel);

            _applicationListView = new ListView
            {
                Location = new Point(20, 45),
                Size = new Size(560, 110),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _applicationListView.Columns.Add("확장자", 100);
            _applicationListView.Columns.Add("응용 프로그램 경로", 450);
            _customApplicationGroup.Controls.Add(_applicationListView);

            _addApplicationButton = new Button
            {
                Text = "추가/수정",
                Location = new Point(590, 45),
                Width = 80,
                Height = 30
            };
            _addApplicationButton.Click += OnAddApplicationClick;
            _customApplicationGroup.Controls.Add(_addApplicationButton);

            _removeApplicationButton = new Button
            {
                Text = "제거",
                Location = new Point(590, 85),
                Width = 80,
                Height = 30
            };
            _removeApplicationButton.Click += OnRemoveApplicationClick;
            _customApplicationGroup.Controls.Add(_removeApplicationButton);

            // File groups group
            _fileGroupsGroup = new GroupBox
            {
                Text = "파일 그룹 관리",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var fileGroupsLabel = new Label
            {
                Text = "함께 열리는 파일 그룹을 관리합니다. 그룹의 파일 중 하나를 열면 모든 파일이 함께 열립니다:",
                Location = new Point(20, 20),
                Size = new Size(600, 20)
            };
            _fileGroupsGroup.Controls.Add(fileGroupsLabel);

            _fileGroupsListView = new ListView
            {
                Location = new Point(20, 45),
                Size = new Size(560, 110),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _fileGroupsListView.Columns.Add("그룹 이름", 150);
            _fileGroupsListView.Columns.Add("파일 수", 100);
            _fileGroupsListView.Columns.Add("파일 목록", 300);
            _fileGroupsListView.DoubleClick += OnFileGroupsListViewDoubleClick;
            _fileGroupsGroup.Controls.Add(_fileGroupsListView);

            _addGroupButton = new Button
            {
                Text = "추가",
                Location = new Point(590, 45),
                Width = 80,
                Height = 30
            };
            _addGroupButton.Click += OnAddGroupClick;
            _fileGroupsGroup.Controls.Add(_addGroupButton);

            _renameGroupButton = new Button
            {
                Text = "이름 변경",
                Location = new Point(590, 85),
                Width = 80,
                Height = 30
            };
            _renameGroupButton.Click += OnRenameGroupClick;
            _fileGroupsGroup.Controls.Add(_renameGroupButton);

            _manageGroupFilesButton = new Button
            {
                Text = "파일 관리",
                Location = new Point(590, 125),
                Width = 80,
                Height = 30
            };
            _manageGroupFilesButton.Click += OnManageGroupFilesClick;
            _fileGroupsGroup.Controls.Add(_manageGroupFilesButton);

            _removeGroupButton = new Button
            {
                Text = "삭제",
                Location = new Point(590, 165),
                Width = 80,
                Height = 30
            };
            _removeGroupButton.Click += OnRemoveGroupClick;
            _fileGroupsGroup.Controls.Add(_removeGroupButton);

            // Log textbox
            _logTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            // Close button
            _closeButton = new Button
            {
                Text = "닫기",
                Width = 100,
                Height = 35,
                Margin = new Padding(5, 0, 0, 0)
            };
            _closeButton.Click += (s, e) => this.Close();

            // Reset all button
            _resetAllButton = new Button
            {
                Text = "모든 설정 초기화",
                Width = 150,
                Height = 35,
                Margin = new Padding(5, 0, 0, 0),
                BackColor = Color.LightCoral
            };
            _resetAllButton.Click += OnResetAllClick;

            buttonPanel.Controls.Add(_closeButton);
            buttonPanel.Controls.Add(_resetAllButton);

            mainPanel.Controls.Add(_startupGroup, 0, 0);
            mainPanel.Controls.Add(_fileAssociationGroup, 0, 1);
            mainPanel.Controls.Add(_customApplicationGroup, 0, 2);
            mainPanel.Controls.Add(_fileGroupsGroup, 0, 3);
            mainPanel.Controls.Add(_logTextBox, 0, 4);
            mainPanel.Controls.Add(buttonPanel, 0, 5);

            this.Controls.Add(mainPanel);

            AddLog("UnlockOpenFile 설정을 열었습니다.");
            AddLog("관리자 권한이 필요한 작업의 경우 UAC 프롬프트가 표시될 수 있습니다.");
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Check startup registration
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                if (key != null)
                {
                    var value = key.GetValue("UnlockOpenFile");
                    _startupCheckBox.Checked = value != null;
                }

                // Load custom application paths
                LoadCustomApplications();

                // Load file groups
                LoadFileGroups();
            }
            catch (Exception ex)
            {
                AddLog($"설정 로드 오류: {ex.Message}");
            }
        }

        private void LoadCustomApplications()
        {
            _applicationListView.Items.Clear();
            var apps = ApplicationSettings.GetAllApplicationPaths();
            foreach (var app in apps)
            {
                var item = new ListViewItem(app.Key);
                item.SubItems.Add(app.Value);
                _applicationListView.Items.Add(item);
            }
        }

        private void OnStartupCheckChanged(object? sender, EventArgs e)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (_startupCheckBox.Checked)
                    {
                        var exePath = Application.ExecutablePath;
                        key.SetValue("UnlockOpenFile", exePath);
                        AddLog("시작 프로그램에 등록되었습니다.");
                    }
                    else
                    {
                        key.DeleteValue("UnlockOpenFile", false);
                        AddLog("시작 프로그램에서 제거되었습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"시작 프로그램 설정 오류: {ex.Message}");
                _startupCheckBox.Checked = !_startupCheckBox.Checked;
            }
        }

        private void RegisterFileAssociation(string extension, string description)
        {
            try
            {
                var exePath = Application.ExecutablePath;
                var progId = $"UnlockOpenFile{extension}";

                // Save the original association before overwriting
                string? originalProgId = null;
                try
                {
                    using var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}");
                    if (extKey != null)
                    {
                        originalProgId = extKey.GetValue("")?.ToString();
                        // Only save if it's not already our ProgId and not empty
                        if (!string.IsNullOrEmpty(originalProgId) && 
                            !originalProgId.StartsWith("UnlockOpenFile"))
                        {
                            ApplicationSettings.SaveOriginalAssociation(extension, originalProgId);
                            AddLog($"{extension} 파일의 이전 연결 정보 저장됨: {originalProgId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"이전 연결 정보 저장 오류: {ex.Message}");
                }

                // Register file association in HKEY_CURRENT_USER (doesn't require admin)
                using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
                {
                    extKey.SetValue("", progId);
                }

                using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
                {
                    progIdKey.SetValue("", $"{description} (UnlockOpenFile)");
                    
                    using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
                    {
                        iconKey.SetValue("", $"{exePath},0");
                    }

                    using (var commandKey = progIdKey.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }

                AddLog($"{extension} 파일이 이 프로그램과 연결되었습니다.");
                
                // Prompt user to restart Explorer
                PromptExplorerRestart();
            }
            catch (Exception ex)
            {
                AddLog($"파일 연결 오류: {ex.Message}");
                MessageBox.Show($"파일 연결에 실패했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUnregisterClick(object? sender, EventArgs e)
        {
            try
            {
                var extensions = new[] { ".xlsx", ".csv" };
                foreach (var ext in extensions)
                {
                    try
                    {
                        // Check if there's an original association to restore
                        string? originalProgId = ApplicationSettings.GetOriginalAssociation(ext);
                        
                        if (!string.IsNullOrEmpty(originalProgId))
                        {
                            // Restore the original association
                            using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}"))
                            {
                                extKey.SetValue("", originalProgId);
                            }
                            AddLog($"{ext} 파일의 이전 연결 복원됨: {originalProgId}");
                            
                            // Remove the saved original association
                            ApplicationSettings.RemoveOriginalAssociation(ext);
                        }
                        else
                        {
                            // No original association saved, just delete the current one
                            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", false);
                            AddLog($"{ext} 파일 연결이 제거되었습니다.");
                        }
                        
                        // Clean up our ProgId
                        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\UnlockOpenFile{ext}", false);
                    }
                    catch { }
                }
                AddLog("파일 연결 해제 완료.");
                
                // Prompt user to restart Explorer
                PromptExplorerRestart();
            }
            catch (Exception ex)
            {
                AddLog($"파일 연결 해제 오류: {ex.Message}");
            }
        }

        private void OnAddApplicationClick(object? sender, EventArgs e)
        {
            // Create a simple input dialog for extension
            using var extensionDialog = new Form
            {
                Text = "확장자 입력",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "파일 확장자를 입력하세요 (예: .txt, .xlsx):",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 340
            };

            var okButton = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 75),
                Width = 80
            };

            var cancelButton = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 75),
                Width = 80
            };

            extensionDialog.Controls.Add(label);
            extensionDialog.Controls.Add(textBox);
            extensionDialog.Controls.Add(okButton);
            extensionDialog.Controls.Add(cancelButton);
            extensionDialog.AcceptButton = okButton;
            extensionDialog.CancelButton = cancelButton;

            if (extensionDialog.ShowDialog() == DialogResult.OK)
            {
                var extension = textBox.Text.Trim();
                if (string.IsNullOrEmpty(extension))
                {
                    MessageBox.Show("확장자를 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }

                // Open file dialog to select application
                using var openFileDialog = new OpenFileDialog
                {
                    Title = $"{extension} 파일을 열 응용 프로그램 선택",
                    Filter = "실행 파일 (*.exe)|*.exe|모든 파일 (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ApplicationSettings.SetApplicationPath(extension, openFileDialog.FileName);
                        AddLog($"{extension} 파일을 {System.IO.Path.GetFileName(openFileDialog.FileName)}(으)로 여는 설정이 저장되었습니다.");
                        LoadCustomApplications();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"응용 프로그램 설정 오류: {ex.Message}");
                        MessageBox.Show($"응용 프로그램 설정에 실패했습니다: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnRemoveApplicationClick(object? sender, EventArgs e)
        {
            if (_applicationListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("제거할 항목을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = _applicationListView.SelectedItems[0];
            var extension = selectedItem.Text;

            var result = MessageBox.Show(
                $"{extension} 확장자의 사용자 지정 응용 프로그램 설정을 제거하시겠습니까?",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    ApplicationSettings.RemoveApplicationPath(extension);
                    AddLog($"{extension} 확장자의 사용자 지정 응용 프로그램 설정이 제거되었습니다.");
                    LoadCustomApplications();
                }
                catch (Exception ex)
                {
                    AddLog($"응용 프로그램 설정 제거 오류: {ex.Message}");
                    MessageBox.Show($"설정 제거에 실패했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnResetAllClick(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "모든 설정을 초기화하시겠습니까?\n\n다음 설정이 삭제됩니다:\n" +
                "- 시작 프로그램 등록\n" +
                "- 파일 연결 (.xlsx, .csv)\n" +
                "- 사용자 지정 응용 프로그램 설정\n" +
                "- 파일 그룹\n" +
                "- 최근 파일 목록\n\n" +
                "이 작업은 되돌릴 수 없습니다.",
                "모든 설정 초기화",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Remove startup registration
                    try
                    {
                        using var startupKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                        if (startupKey != null)
                        {
                            startupKey.DeleteValue("UnlockOpenFile", false);
                        }
                    }
                    catch { }

                    // Remove file associations
                    var extensions = new[] { ".xlsx", ".csv" };
                    foreach (var ext in extensions)
                    {
                        try
                        {
                            // Restore original associations if they exist
                            string? originalProgId = ApplicationSettings.GetOriginalAssociation(ext);
                            if (!string.IsNullOrEmpty(originalProgId))
                            {
                                using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}"))
                                {
                                    extKey.SetValue("", originalProgId);
                                }
                            }
                            else
                            {
                                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", false);
                            }
                            
                            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\UnlockOpenFile{ext}", false);
                        }
                        catch { }
                    }

                    // Clear all custom application paths
                    ApplicationSettings.ClearAllSettings();

                    // Clear all file groups
                    FileGroupManager.ClearAllGroups();

                    // Clear recent files
                    RecentFilesManager.ClearRecentFiles();

                    AddLog("모든 설정이 초기화되었습니다.");
                    MessageBox.Show(
                        "모든 설정이 성공적으로 초기화되었습니다.\n설정 창이 닫힙니다.",
                        "완료",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    this.Close();
                }
                catch (Exception ex)
                {
                    AddLog($"설정 초기화 오류: {ex.Message}");
                    MessageBox.Show($"설정 초기화에 실패했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnClearRecentFilesClick(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "최근 파일 목록을 모두 지우시겠습니까?",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    RecentFilesManager.ClearRecentFiles();
                    AddLog("최근 파일 목록이 지워졌습니다.");
                    MessageBox.Show(
                        "최근 파일 목록이 성공적으로 지워졌습니다.",
                        "완료",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AddLog($"최근 파일 목록 지우기 오류: {ex.Message}");
                    MessageBox.Show($"최근 파일 목록 지우기에 실패했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

        private void PromptExplorerRestart()
        {
            var result = MessageBox.Show(
                "파일 연결 변경 사항을 적용하려면 Windows 탐색기를 재시작해야 합니다.\n\n" +
                "지금 탐색기를 재시작하시겠습니까?\n\n" +
                "참고: 탐색기를 재시작하면 열려 있는 탐색기 창이 모두 닫힙니다.",
                "탐색기 재시작",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                AddLog("Windows 탐색기를 재시작하는 중...");
                
                bool success = ExplorerHelper.RestartExplorer();
                
                if (success)
                {
                    AddLog("Windows 탐색기가 성공적으로 재시작되었습니다.");
                    MessageBox.Show(
                        "Windows 탐색기가 재시작되었습니다.\n파일 연결 변경 사항이 적용되었습니다.",
                        "완료",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    AddLog("탐색기 재시작 중 오류가 발생했습니다.");
                    MessageBox.Show(
                        "탐색기 재시작에 실패했습니다.\n수동으로 로그아웃 후 다시 로그인하거나 PC를 재시작해주세요.",
                        "오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                AddLog("탐색기 재시작을 건너뜁니다. 변경 사항을 적용하려면 나중에 로그아웃 후 다시 로그인하거나 PC를 재시작하세요.");
            }
        }

        private void LoadFileGroups()
        {
            _fileGroupsListView.Items.Clear();
            var groups = FileGroupManager.GetAllGroups();
            foreach (var group in groups)
            {
                var item = new ListViewItem(group.Key);
                item.SubItems.Add(group.Value.Count.ToString());
                var fileNames = string.Join(", ", group.Value.Select(f => System.IO.Path.GetFileName(f)));
                if (fileNames.Length > 50)
                {
                    fileNames = fileNames.Substring(0, 47) + "...";
                }
                item.SubItems.Add(fileNames);
                _fileGroupsListView.Items.Add(item);
            }
        }

        private void OnAddGroupClick(object? sender, EventArgs e)
        {
            // Create a simple input dialog for group name
            using var groupDialog = new Form
            {
                Text = "그룹 추가",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "그룹 이름을 입력하세요:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 340
            };

            var okButton = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 75),
                Width = 80
            };

            var cancelButton = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 75),
                Width = 80
            };

            groupDialog.Controls.Add(label);
            groupDialog.Controls.Add(textBox);
            groupDialog.Controls.Add(okButton);
            groupDialog.Controls.Add(cancelButton);
            groupDialog.AcceptButton = okButton;
            groupDialog.CancelButton = cancelButton;

            if (groupDialog.ShowDialog() == DialogResult.OK)
            {
                var groupName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(groupName))
                {
                    MessageBox.Show("그룹 이름을 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if group already exists
                var existingGroups = FileGroupManager.GetAllGroups();
                if (existingGroups.ContainsKey(groupName))
                {
                    MessageBox.Show($"'{groupName}' 그룹이 이미 존재합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Open file dialog to select files for the group
                using var openFileDialog = new OpenFileDialog
                {
                    Title = $"'{groupName}' 그룹에 추가할 파일 선택",
                    Filter = "모든 파일 (*.*)|*.*",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (openFileDialog.FileNames.Length == 0)
                    {
                        MessageBox.Show("파일을 하나 이상 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        foreach (var filePath in openFileDialog.FileNames)
                        {
                            FileGroupManager.AddFileToGroup(groupName, filePath);
                        }
                        AddLog($"'{groupName}' 그룹에 {openFileDialog.FileNames.Length}개 파일이 추가되었습니다.");
                        LoadFileGroups();
                    }
                    catch (Exception ex)
                    {
                        AddLog($"그룹 추가 오류: {ex.Message}");
                        MessageBox.Show($"그룹 추가에 실패했습니다: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnManageGroupFilesClick(object? sender, EventArgs e)
        {
            if (_fileGroupsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("관리할 그룹을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = _fileGroupsListView.SelectedItems[0];
            var groupName = selectedItem.Text;

            // Create a dialog to manage files in the group
            using var manageDialog = new Form
            {
                Text = $"'{groupName}' 그룹 파일 관리",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var filesListView = new ListView
            {
                Location = new Point(20, 20),
                Size = new Size(450, 380),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            filesListView.Columns.Add("파일명", 150);
            filesListView.Columns.Add("경로", 290);

            // Load files from the group
            var groupFiles = FileGroupManager.GetGroupFiles(groupName);
            foreach (var filePath in groupFiles)
            {
                var item = new ListViewItem(System.IO.Path.GetFileName(filePath));
                item.SubItems.Add(filePath);
                item.Tag = filePath;
                filesListView.Items.Add(item);
            }

            manageDialog.Controls.Add(filesListView);

            var addButton = new Button
            {
                Text = "파일 추가",
                Location = new Point(480, 20),
                Width = 100,
                Height = 30
            };
            addButton.Click += (s, ev) =>
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Title = $"'{groupName}' 그룹에 추가할 파일 선택",
                    Filter = "모든 파일 (*.*)|*.*",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var filePath in openFileDialog.FileNames)
                    {
                        try
                        {
                            FileGroupManager.AddFileToGroup(groupName, filePath);
                            
                            // Check if file is already in the list
                            bool exists = false;
                            foreach (ListViewItem existingItem in filesListView.Items)
                            {
                                if (existingItem.Tag?.ToString()?.Equals(filePath, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            
                            if (!exists)
                            {
                                var item = new ListViewItem(System.IO.Path.GetFileName(filePath));
                                item.SubItems.Add(filePath);
                                item.Tag = filePath;
                                filesListView.Items.Add(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"파일 추가 오류: {ex.Message}", "오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            manageDialog.Controls.Add(addButton);

            var addFromRecentButton = new Button
            {
                Text = "최근 목록에서\n추가",
                Location = new Point(590, 20),
                Width = 90,
                Height = 40
            };
            addFromRecentButton.Click += (s, ev) =>
            {
                // Create a dialog to select from recent files
                using var recentDialog = new Form
                {
                    Text = "최근 파일에서 선택",
                    Size = new Size(600, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var recentListView = new ListView
                {
                    Location = new Point(20, 20),
                    Size = new Size(540, 280),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    CheckBoxes = true
                };
                recentListView.Columns.Add("파일명", 150);
                recentListView.Columns.Add("경로", 380);

                // Load recent files
                var recentFiles = RecentFilesManager.GetRecentFiles();
                foreach (var filePath in recentFiles)
                {
                    var item = new ListViewItem(System.IO.Path.GetFileName(filePath));
                    item.SubItems.Add(filePath);
                    item.Tag = filePath;
                    recentListView.Items.Add(item);
                }

                recentDialog.Controls.Add(recentListView);

                var okButton = new Button
                {
                    Text = "추가",
                    Location = new Point(400, 310),
                    Width = 80,
                    Height = 30,
                    DialogResult = DialogResult.OK
                };
                recentDialog.Controls.Add(okButton);

                var cancelButton = new Button
                {
                    Text = "취소",
                    Location = new Point(490, 310),
                    Width = 80,
                    Height = 30,
                    DialogResult = DialogResult.Cancel
                };
                recentDialog.Controls.Add(cancelButton);

                recentDialog.AcceptButton = okButton;
                recentDialog.CancelButton = cancelButton;

                if (recentDialog.ShowDialog() == DialogResult.OK)
                {
                    var addedCount = 0;
                    foreach (ListViewItem item in recentListView.Items)
                    {
                        if (item.Checked)
                        {
                            var filePath = item.Tag?.ToString();
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                try
                                {
                                    FileGroupManager.AddFileToGroup(groupName, filePath);
                                    
                                    // Check if file is already in the list
                                    bool exists = false;
                                    foreach (ListViewItem existingItem in filesListView.Items)
                                    {
                                        if (existingItem.Tag?.ToString()?.Equals(filePath, StringComparison.OrdinalIgnoreCase) == true)
                                        {
                                            exists = true;
                                            break;
                                        }
                                    }
                                    
                                    if (!exists)
                                    {
                                        var newItem = new ListViewItem(System.IO.Path.GetFileName(filePath));
                                        newItem.SubItems.Add(filePath);
                                        newItem.Tag = filePath;
                                        filesListView.Items.Add(newItem);
                                        addedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"파일 추가 오류: {ex.Message}", "오류",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                    
                    if (addedCount > 0)
                    {
                        MessageBox.Show($"{addedCount}개의 파일이 추가되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
            manageDialog.Controls.Add(addFromRecentButton);

            var removeButton = new Button
            {
                Text = "파일 제거",
                Location = new Point(480, 70),
                Width = 100,
                Height = 30
            };
            removeButton.Click += (s, ev) =>
            {
                if (filesListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("제거할 파일을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedFile = filesListView.SelectedItems[0];
                var filePath = selectedFile.Tag?.ToString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    FileGroupManager.RemoveFileFromGroup(groupName, filePath);
                    filesListView.Items.Remove(selectedFile);
                }
            };
            manageDialog.Controls.Add(removeButton);

            var closeButton = new Button
            {
                Text = "닫기",
                Location = new Point(580, 420),
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            manageDialog.Controls.Add(closeButton);
            manageDialog.AcceptButton = closeButton;

            if (manageDialog.ShowDialog() == DialogResult.OK)
            {
                AddLog($"'{groupName}' 그룹 파일 관리 완료.");
                LoadFileGroups();
            }
        }

        private void OnRemoveGroupClick(object? sender, EventArgs e)
        {
            if (_fileGroupsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("제거할 그룹을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = _fileGroupsListView.SelectedItems[0];
            var groupName = selectedItem.Text;

            var result = MessageBox.Show(
                $"'{groupName}' 그룹을 삭제하시겠습니까?",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    FileGroupManager.DeleteGroup(groupName);
                    AddLog($"'{groupName}' 그룹이 삭제되었습니다.");
                    LoadFileGroups();
                }
                catch (Exception ex)
                {
                    AddLog($"그룹 삭제 오류: {ex.Message}");
                    MessageBox.Show($"그룹 삭제에 실패했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnRenameGroupClick(object? sender, EventArgs e)
        {
            if (_fileGroupsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("이름을 변경할 그룹을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = _fileGroupsListView.SelectedItems[0];
            var oldGroupName = selectedItem.Text;

            // Create a simple input dialog for new group name
            using var renameDialog = new Form
            {
                Text = "그룹 이름 변경",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "새 그룹 이름을 입력하세요:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 340,
                Text = oldGroupName
            };

            var okButton = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 75),
                Width = 80
            };

            var cancelButton = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 75),
                Width = 80
            };

            renameDialog.Controls.Add(label);
            renameDialog.Controls.Add(textBox);
            renameDialog.Controls.Add(okButton);
            renameDialog.Controls.Add(cancelButton);
            renameDialog.AcceptButton = okButton;
            renameDialog.CancelButton = cancelButton;

            if (renameDialog.ShowDialog() == DialogResult.OK)
            {
                var newGroupName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(newGroupName))
                {
                    MessageBox.Show("그룹 이름을 입력해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (newGroupName.Equals(oldGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    return; // No change
                }

                // Check if new group name already exists
                var existingGroups = FileGroupManager.GetAllGroups();
                if (existingGroups.ContainsKey(newGroupName))
                {
                    MessageBox.Show($"'{newGroupName}' 그룹이 이미 존재합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    FileGroupManager.RenameGroup(oldGroupName, newGroupName);
                    AddLog($"'{oldGroupName}' 그룹이 '{newGroupName}'(으)로 이름이 변경되었습니다.");
                    LoadFileGroups();
                }
                catch (Exception ex)
                {
                    AddLog($"그룹 이름 변경 오류: {ex.Message}");
                    MessageBox.Show($"그룹 이름 변경에 실패했습니다: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnFileGroupsListViewDoubleClick(object? sender, EventArgs e)
        {
            // Trigger rename on double-click
            OnRenameGroupClick(sender, e);
        }
    }
}
