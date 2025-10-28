using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace UnlockOpenFile
{
    static class Program
    {
        private const string MutexName = "UnlockOpenFile_SingleInstance_Mutex";
        private const string PipeName = "UnlockOpenFile_IPC_Pipe";
        private static Mutex? _mutex;
        private static MainForm? _mainForm;
        private static SettingsForm? _settingsForm;
        private static TrayApplicationContext? _trayContext;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // Try to acquire mutex for single instance
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                if (args.Length > 0)
                {
                    // Send file path to existing instance
                    SendFilePathToExistingInstance(args[0]);
                }
                else
                {
                    // If no MainForm is running, just show settings
                    // Otherwise send command to show settings
                    if (!TrySendCommandToExistingInstance("SHOW_SETTINGS"))
                    {
                        // No main form is running, just show settings directly
                        Application.Run(new SettingsForm());
                    }
                }
                return;
            }

            try
            {
                // Always start IPC server to handle file open requests
                StartIPCServer();

                // Check if started with --startup argument (Windows startup)
                bool isStartupMode = args.Length > 0 && args[0] == "--startup";

                if (isStartupMode)
                {
                    // Start minimized in tray when launched at Windows startup
                    // Show tray icon with option to open main form
                    _trayContext = new TrayApplicationContext();
                    Application.Run(_trayContext);
                }
                else if (args.Length > 0)
                {
                    // If arguments are provided, open file in main form
                    _mainForm = new MainForm();
                    _mainForm.OpenFile(args[0]);
                    Application.Run(_mainForm);
                }
                else
                {
                    // Show settings form if no file is specified
                    // IPC server is running, so file open requests will be handled
                    _settingsForm = new SettingsForm();
                    Application.Run(_settingsForm);
                }
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        private static void StartIPCServer()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                        server.WaitForConnection();

                        using var reader = new StreamReader(server, Encoding.UTF8);
                        var message = reader.ReadToEnd();

                        if (!string.IsNullOrEmpty(message))
                        {
                            if (message.StartsWith("FILE:"))
                            {
                                var filePath = message.Substring(5);
                                
                                // Check if MainForm exists and is not disposed
                                if (_mainForm != null && !_mainForm.IsDisposed)
                                {
                                    // MainForm exists, open file in it
                                    _mainForm.Invoke(() =>
                                    {
                                        _mainForm.OpenFile(filePath);
                                        _mainForm.Show();
                                        _mainForm.WindowState = FormWindowState.Normal;
                                        _mainForm.BringToFront();
                                    });
                                }
                                else if (_trayContext != null && _trayContext.HiddenForm != null && !_trayContext.HiddenForm.IsDisposed)
                                {
                                    // Tray context is running, use the hidden form to marshal to UI thread
                                    _trayContext.HiddenForm.Invoke(() =>
                                    {
                                        _trayContext.ShowMainForm();
                                        var mainForm = _trayContext.MainForm;
                                        if (mainForm != null)
                                        {
                                            _mainForm = mainForm; // Update the reference
                                            mainForm.OpenFile(filePath);
                                            mainForm.Show();
                                            mainForm.WindowState = FormWindowState.Normal;
                                            mainForm.BringToFront();
                                        }
                                    });
                                }
                                else if (_settingsForm != null && !_settingsForm.IsDisposed)
                                {
                                    // MainForm doesn't exist but SettingsForm is running
                                    // Create MainForm on the UI thread using SettingsForm's synchronization context
                                    _settingsForm.Invoke(() =>
                                    {
                                        _mainForm = new MainForm();
                                        _mainForm.OpenFile(filePath);
                                        _mainForm.Show();
                                        _mainForm.WindowState = FormWindowState.Normal;
                                        _mainForm.BringToFront();
                                    });
                                }
                            }
                            else if (message == "SHOW_SETTINGS")
                            {
                                if (_mainForm != null && !_mainForm.IsDisposed)
                                {
                                    _mainForm.Invoke(() =>
                                    {
                                        var settingsForm = new SettingsForm();
                                        settingsForm.ShowDialog();
                                    });
                                }
                                else if (_trayContext != null && _trayContext.HiddenForm != null && !_trayContext.HiddenForm.IsDisposed)
                                {
                                    // Tray context is running, use the hidden form to marshal to UI thread
                                    _trayContext.HiddenForm.Invoke(() =>
                                    {
                                        var settingsForm = new SettingsForm();
                                        settingsForm.ShowDialog();
                                    });
                                }
                                else if (_settingsForm != null && !_settingsForm.IsDisposed)
                                {
                                    // SettingsForm is already shown, just bring it to front
                                    _settingsForm.Invoke(() =>
                                    {
                                        _settingsForm.Show();
                                        _settingsForm.WindowState = FormWindowState.Normal;
                                        _settingsForm.BringToFront();
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors and continue listening
                    }
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        private static void SendFilePathToExistingInstance(string filePath)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(5000); // 5 second timeout

                using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
                writer.Write($"FILE:{Path.GetFullPath(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"실행 중인 프로그램과 통신할 수 없습니다: {ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool TrySendCommandToExistingInstance(string command)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000); // 1 second timeout

                using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
                writer.Write(command);
                return true;
            }
            catch
            {
                // No IPC server running
                return false;
            }
        }
    }
}
