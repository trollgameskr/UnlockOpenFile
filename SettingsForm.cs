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
        private TextBox _logTextBox = null!;
        private GroupBox _fileAssociationGroup = null!;
        private GroupBox _startupGroup = null!;

        public SettingsForm()
        {
            InitializeComponents();
            LoadCurrentSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "UnlockOpenFile - 설정";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
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

            // Log textbox
            _logTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };

            // Close button
            _closeButton = new Button
            {
                Text = "닫기",
                Dock = DockStyle.Right,
                Width = 100
            };
            _closeButton.Click += (s, e) => this.Close();

            mainPanel.Controls.Add(_startupGroup, 0, 0);
            mainPanel.Controls.Add(_fileAssociationGroup, 0, 1);
            mainPanel.Controls.Add(_logTextBox, 0, 2);
            mainPanel.Controls.Add(_closeButton, 0, 3);

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
            }
            catch (Exception ex)
            {
                AddLog($"설정 로드 오류: {ex.Message}");
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
