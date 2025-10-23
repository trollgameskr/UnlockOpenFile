using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public static class ApplicationSettings
    {
        private const string RegistryPath = @"Software\UnlockOpenFile\Applications";
        private const string OriginalAssociationsPath = @"Software\UnlockOpenFile\OriginalAssociations";

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

        public static void SaveOriginalAssociation(string extension, string progId)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(OriginalAssociationsPath);
                if (key != null)
                {
                    key.SetValue(extension, progId);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"원본 파일 연결 정보 저장 실패: {ex.Message}", ex);
            }
        }

        public static string? GetOriginalAssociation(string extension)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(OriginalAssociationsPath);
                if (key != null)
                {
                    return key.GetValue(extension)?.ToString();
                }
            }
            catch
            {
                // Ignore errors and return null
            }
            return null;
        }

        public static void RemoveOriginalAssociation(string extension)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(OriginalAssociationsPath, true);
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

        public static Dictionary<string, string> GetAllOriginalAssociations()
        {
            var result = new Dictionary<string, string>();
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(OriginalAssociationsPath);
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var value = key.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(value))
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
    }
}
