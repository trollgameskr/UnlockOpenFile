using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public static class ApplicationSettings
    {
        private const string RegistryPath = @"Software\UnlockOpenFile\Applications";

        public static void SetApplicationPath(string extension, string applicationPath)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                if (key != null)
                {
                    key.SetValue(extension, applicationPath);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"응용 프로그램 경로 저장 실패: {ex.Message}", ex);
            }
        }

        public static string? GetApplicationPath(string extension)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key != null)
                {
                    var value = key.GetValue(extension)?.ToString();
                    if (!string.IsNullOrEmpty(value) && System.IO.File.Exists(value))
                    {
                        return value;
                    }
                }
            }
            catch
            {
                // Ignore errors and return null
            }
            return null;
        }

        public static void RemoveApplicationPath(string extension)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                if (key != null)
                {
                    key.DeleteValue(extension, false);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        public static Dictionary<string, string> GetAllApplicationPaths()
        {
            var result = new Dictionary<string, string>();
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var value = key.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(value) && System.IO.File.Exists(value))
                        {
                            result[valueName] = value;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors and return empty dictionary
            }
            return result;
        }

        public static void ClearAllSettings()
        {
            try
            {
                // Delete the entire registry key and all its values
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\UnlockOpenFile", false);
            }
            catch
            {
                // Ignore errors if key doesn't exist
            }
        }
    }
}
