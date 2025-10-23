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
        private TextBox _logTextBox = null!;
        private GroupBox _fileAssociationGroup = null!;
        private GroupBox _startupGroup = null!;
        private GroupBox _customApplicationGroup = null!;
        private ListView _applicationListView = null!;
        private Button _addApplicationButton = null!;
        private Button _removeApplicationButton = null!;

        public SettingsForm()
        {
            InitializeComponents();
            LoadCurrentSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "UnlockOpenFile - 설정";
            this.Size = new Size(700, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
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
                Size = new Size(600, 40),
                ForeColor = Color.Gray
            };
            _startupGroup.Controls.Add(startupLabel);

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
            mainPanel.Controls.Add(_logTextBox, 0, 3);
            mainPanel.Controls.Add(buttonPanel, 0, 4);

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
                AddLog("변경 사항을 적용하려면 탐색기를 새로 고치거나 로그아웃 후 다시 로그인하세요.");
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
                        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", false);
                        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\UnlockOpenFile{ext}", false);
                    }
                    catch { }
                }
                AddLog("파일 연결이 해제되었습니다.");
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
                "- 사용자 지정 응용 프로그램 설정\n\n" +
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
                            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", false);
                            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\UnlockOpenFile{ext}", false);
                        }
                        catch { }
                    }

                    // Clear all custom application paths
                    ApplicationSettings.ClearAllSettings();

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
