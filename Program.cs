using System;
using System.Windows.Forms;

namespace UnlockOpenFile
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            // If arguments are provided, process the file
            if (args.Length > 0)
            {
                try
                {
                    Application.Run(new FileOpenerForm(args[0]));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"오류 발생: {ex.Message}\n\n파일 경로: {args[0]}", 
                        "UnlockOpenFile - 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Show settings form if no file is specified
                Application.Run(new SettingsForm());
            }
        }
    }
}
