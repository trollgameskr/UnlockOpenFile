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
        private Form? _hiddenForm; // Hidden form for cross-thread marshalling

        public TrayApplicationContext()
        {
            // Create a hidden form for cross-thread marshalling
            _hiddenForm = new HiddenInvokeForm();
            _hiddenForm.Show();
            _hiddenForm.Hide();
            
            InitializeTrayIcon();
        }

        /// <summary>
        /// Hidden form used only for cross-thread marshalling
        /// </summary>
        private class HiddenInvokeForm : Form
        {
            public HiddenInvokeForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                Width = 0;
                Height = 0;
            }

            protected override void SetVisibleCore(bool value)
            {
                // Prevent the form from ever becoming visible
                base.SetVisibleCore(false);
            }
        }

        /// <summary>
        /// Gets the current main form instance, or null if not created
        /// </summary>
        public new MainForm? MainForm => _mainForm;

        /// <summary>
        /// Gets the hidden form used for cross-thread marshalling
        /// </summary>
        public Form? HiddenForm => _hiddenForm;

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
            
            if (_hiddenForm != null && !_hiddenForm.IsDisposed)
            {
                _hiddenForm.Close();
            }
            
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon?.Dispose();
                _mainForm?.Dispose();
                _hiddenForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
