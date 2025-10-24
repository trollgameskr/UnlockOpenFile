using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public static class FileGroupManager
    {
        private const string RegistryPath = @"Software\UnlockOpenFile\FileGroups";

        /// <summary>
        /// Adds a file to a group. If the file is already in another group, it will be removed from that group.
        /// </summary>
        public static void AddFileToGroup(string groupName, string filePath)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("그룹 이름이 비어있습니다.", nameof(groupName));
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("파일 경로가 비어있습니다.", nameof(filePath));

            try
            {
                // Remove file from any existing group first
                RemoveFileFromAllGroups(filePath);

                // Get current group files
                var groupFiles = GetGroupFiles(groupName);
                
                // Add the file if not already present
                if (!groupFiles.Contains(filePath, StringComparer.OrdinalIgnoreCase))
                {
                    groupFiles.Add(filePath);
                    SaveGroup(groupName, groupFiles);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"파일을 그룹에 추가하는 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Removes a file from a specific group.
        /// </summary>
        public static void RemoveFileFromGroup(string groupName, string filePath)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            try
            {
                var groupFiles = GetGroupFiles(groupName);
                groupFiles.RemoveAll(f => f.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                
                if (groupFiles.Count == 0)
                {
                    // Delete the group if it's empty
                    DeleteGroup(groupName);
                }
                else
                {
                    SaveGroup(groupName, groupFiles);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Removes a file from all groups it belongs to.
        /// </summary>
        public static void RemoveFileFromAllGroups(string filePath)
        {
            try
            {
                var allGroups = GetAllGroups();
                foreach (var groupName in allGroups.Keys)
                {
                    RemoveFileFromGroup(groupName, filePath);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Gets all files in a specific group.
        /// </summary>
        public static List<string> GetGroupFiles(string groupName)
        {
            var files = new List<string>();
            
            if (string.IsNullOrWhiteSpace(groupName))
                return files;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey($@"{RegistryPath}\{groupName}");
                if (key != null)
                {
                    var valueNames = key.GetValueNames()
                        .OrderBy(name => int.TryParse(name, out int index) ? index : int.MaxValue);
                    
                    foreach (var valueName in valueNames)
                    {
                        var filePath = key.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            files.Add(filePath);
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }
            
            return files;
        }

        /// <summary>
        /// Gets the group name that contains the specified file, or null if the file is not in any group.
        /// </summary>
        public static string? GetFileGroup(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            try
            {
                var allGroups = GetAllGroups();
                foreach (var group in allGroups)
                {
                    if (group.Value.Contains(filePath, StringComparer.OrdinalIgnoreCase))
                    {
                        return group.Key;
                    }
                }
            }
            catch
            {
                // Return null on error
            }

            return null;
        }

        /// <summary>
        /// Gets all groups and their files.
        /// </summary>
        public static Dictionary<string, List<string>> GetAllGroups()
        {
            var groups = new Dictionary<string, List<string>>();

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key != null)
                {
                    foreach (var groupName in key.GetSubKeyNames())
                    {
                        var files = GetGroupFiles(groupName);
                        if (files.Count > 0)
                        {
                            groups[groupName] = files;
                        }
                    }
                }
            }
            catch
            {
                // Return empty dictionary on error
            }

            return groups;
        }

        /// <summary>
        /// Deletes a group.
        /// </summary>
        public static void DeleteGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"{RegistryPath}\{groupName}", false);
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Renames a group.
        /// </summary>
        public static void RenameGroup(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("그룹 이름이 비어있습니다.");

            if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var files = GetGroupFiles(oldName);
                if (files.Count > 0)
                {
                    SaveGroup(newName, files);
                    DeleteGroup(oldName);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"그룹 이름 변경 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clears all file groups.
        /// </summary>
        public static void ClearAllGroups()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
            }
            catch
            {
                // Silently fail
            }
        }

        private static void SaveGroup(string groupName, List<string> files)
        {
            try
            {
                // Delete the old group key and create a new one
                Registry.CurrentUser.DeleteSubKeyTree($@"{RegistryPath}\{groupName}", false);
                
                using var key = Registry.CurrentUser.CreateSubKey($@"{RegistryPath}\{groupName}");
                if (key != null)
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        key.SetValue(i.ToString(), files[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"그룹 저장 중 오류 발생: {ex.Message}", ex);
            }
        }
    }
}
