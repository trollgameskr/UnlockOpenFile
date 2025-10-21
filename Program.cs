using System;
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
                // If arguments are provided, open file in main form
                if (args.Length > 0)
                {
                    _mainForm = new MainForm();
                    _mainForm.OpenFile(args[0]);

                    // Start IPC server in background
                    StartIPCServer();

                    Application.Run(_mainForm);
                }
                else
                {
                    // Show settings form if no file is specified
                    // Don't start IPC server in this case
                    Application.Run(new SettingsForm());
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

                        if (_mainForm != null && !string.IsNullOrEmpty(message))
                        {
                            if (message.StartsWith("FILE:"))
                            {
                                var filePath = message.Substring(5);
                                _mainForm.Invoke(() =>
                                {
                                    _mainForm.OpenFile(filePath);
                                    _mainForm.Show();
                                    _mainForm.WindowState = FormWindowState.Normal;
                                    _mainForm.BringToFront();
                                });
                            }
                            else if (message == "SHOW_SETTINGS")
                            {
                                _mainForm.Invoke(() =>
                                {
                                    var settingsForm = new SettingsForm();
                                    settingsForm.ShowDialog();
                                });
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
