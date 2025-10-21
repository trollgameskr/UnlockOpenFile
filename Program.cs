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
                Application.Run(new FileOpenerForm(args[0]));
            }
            else
            {
                // Show settings form if no file is specified
                Application.Run(new SettingsForm());
            }
        }
    }
}
