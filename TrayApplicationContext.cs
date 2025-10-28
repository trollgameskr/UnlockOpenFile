using System;
using System.Drawing;
using System.Windows.Forms;

namespace UnlockOpenFile
{
    /// <summary>
    /// Application context for running the app in the system tray with no visible window
    /// </summary>
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon? _notifyIcon;
        private MainForm? _mainForm;

        public TrayApplicationContext()
        {
            InitializeTrayIcon();
        }

        /// <summary>
        /// Gets the current main form instance, or null if not created
        /// </summary>
        public new MainForm? MainForm => _mainForm;

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "UnlockOpenFile - 파일 관리"
            };

            // Double click to show main form
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            contextMenu.Items.Add("파일 관리 열기", null, (s, e) => ShowMainForm());
            contextMenu.Items.Add("설정", null, (s, e) => ShowSettings());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("종료", null, (s, e) => ExitApplication());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            ShowMainForm();
        }

        /// <summary>
        /// Shows the main form, creating it if necessary
        /// </summary>
        public void ShowMainForm()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                _mainForm = new MainForm();
                _mainForm.FormClosed += (s, e) => _mainForm = null;
                _mainForm.Show();
            }
            else
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.BringToFront();
            }
        }

        private void ShowSettings()
        {
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }

        private void ExitApplication()
        {
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            
            if (_mainForm != null && !_mainForm.IsDisposed)
            {
                _mainForm.Close();
            }
            
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon?.Dispose();
                _mainForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
