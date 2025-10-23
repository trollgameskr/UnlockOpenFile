using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public static class RecentFilesManager
    {
        private const string RegistryPath = @"Software\UnlockOpenFile\RecentFiles";
        private const int MaxRecentFiles = 10;

        public static void AddRecentFile(string filePath)
        {
            try
            {
                var recentFiles = GetRecentFiles();
                
                // Remove if already exists to move it to the top
                recentFiles.RemoveAll(f => f.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                
                // Add to the beginning
                recentFiles.Insert(0, filePath);
                
                // Keep only the most recent files
                if (recentFiles.Count > MaxRecentFiles)
                {
                    recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
                }
                
                // Save to registry
                SaveRecentFiles(recentFiles);
            }
            catch
            {
                // Silently fail if we can't save recent files
            }
        }

        public static List<string> GetRecentFiles()
        {
            var recentFiles = new List<string>();
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key != null)
                {
                    var valueNames = key.GetValueNames()
                        .OrderBy(name => int.TryParse(name, out int index) ? index : int.MaxValue);
                    
                    foreach (var valueName in valueNames)
                    {
                        var filePath = key.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                        {
                            recentFiles.Add(filePath);
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }
            return recentFiles;
        }

        public static void ClearRecentFiles()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
            }
            catch
            {
                // Ignore errors
            }
        }

        private static void SaveRecentFiles(List<string> recentFiles)
        {
            try
            {
                // Delete the old key and create a new one
                Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
                
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                if (key != null)
                {
                    for (int i = 0; i < recentFiles.Count; i++)
                    {
                        key.SetValue(i.ToString(), recentFiles[i]);
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
