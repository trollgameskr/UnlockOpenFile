using System;
using System.Diagnostics;

namespace UnlockOpenFile
{
    public static class ExplorerHelper
    {
        /// <summary>
        /// Restarts Windows Explorer to apply file association changes
        /// </summary>
        /// <returns>True if the restart was successful, false otherwise</returns>
        public static bool RestartExplorer()
        {
            try
            {
                // Find and kill all explorer.exe processes
                var explorerProcesses = Process.GetProcessesByName("explorer");
                foreach (var process in explorerProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000); // Wait up to 5 seconds for the process to exit
                    }
                    catch
                    {
                        // Ignore errors for individual processes
                    }
                }

                // Wait a bit to ensure processes are fully terminated
                System.Threading.Thread.Sleep(500);

                // Start explorer.exe again
                Process.Start("explorer.exe");
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
