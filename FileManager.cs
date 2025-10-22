using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public class FileManager
    {
        private readonly string _originalFilePath;
        private readonly string _tempFilePath;
        private FileSystemWatcher? _fileWatcher;
        private DateTime _lastModified;
        private bool _isModified = false;
        private Process? _openedProcess;
        private Task? _pendingSaveTask;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler? FileModified;
        public event EventHandler? FileSaved;
        public event EventHandler? ProcessExited;

        public FileManager(string originalFilePath)
        {
            if (string.IsNullOrWhiteSpace(originalFilePath))
                throw new ArgumentException("파일 경로가 비어있습니다.", nameof(originalFilePath));
            
            if (!File.Exists(originalFilePath))
                throw new FileNotFoundException("파일을 찾을 수 없습니다.", originalFilePath);
            
            _originalFilePath = originalFilePath;
            _tempFilePath = Path.Combine(Path.GetTempPath(), 
                $"{Path.GetFileNameWithoutExtension(originalFilePath)}_copy_{DateTime.Now.Ticks}{Path.GetExtension(originalFilePath)}");
        }

        public Task<bool> OpenFileAsync()
        {
            try
            {
                // Copy the original file to temp location
                OnStatusChanged("원본 파일 복사 중...");
                File.Copy(_originalFilePath, _tempFilePath, true);
                _lastModified = File.GetLastWriteTime(_tempFilePath);
                
                // Start monitoring the temp file
                StartFileWatcher();
                
                // Open the temp file with the default application
                OnStatusChanged("파일 열기...");
                
                // Get the actual application to open the file, avoiding recursion
                string? appPath = GetActualDefaultApplication(Path.GetExtension(_tempFilePath));
                
                if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                {
                    try
                    {
                        // Open with the specific application
                        _openedProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = appPath,
                            Arguments = $"\"{_tempFilePath}\"",
                            UseShellExecute = false
                        });
                    }
                    catch (Exception ex)
                    {
                        OnStatusChanged($"특정 응용 프로그램으로 열기 실패: {ex.Message}. 기본 방법으로 재시도합니다.");
                        // Fallback to shell execute if specific app fails
                        _openedProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = _tempFilePath,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    // Fallback: use shell execute but this may cause recursion
                    _openedProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = _tempFilePath,
                        UseShellExecute = true
                    });
                }

                OnStatusChanged($"파일이 열렸습니다: {Path.GetFileName(_tempFilePath)}");
                
                // Monitor process exit
                if (_openedProcess != null)
                {
                    _openedProcess.EnableRaisingEvents = true;
                    _openedProcess.Exited += OnProcessExited;
                }
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"오류: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private void StartFileWatcher()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.Dispose();
            }

            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(_tempFilePath)!)
            {
                Filter = Path.GetFileName(_tempFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce multiple change events
                await Task.Delay(500);
                
                var currentModified = File.GetLastWriteTime(_tempFilePath);
                if (currentModified > _lastModified)
                {
                    _lastModified = currentModified;
                    _isModified = true;
                    FileModified?.Invoke(this, EventArgs.Empty);
                    
                    // Save back to original and track the task
                    _pendingSaveTask = SaveToOriginalAsync();
                    await _pendingSaveTask;
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"파일 변경 감지 오류: {ex.Message}");
            }
        }

        private async Task SaveToOriginalAsync()
        {
            try
            {
                OnStatusChanged("변경 사항을 원본에 저장 중...");
                
                // Wait a bit to ensure file is not locked
                await Task.Delay(500);
                
                // Retry logic for locked files
                int retries = 5;
                while (retries > 0)
                {
                    try
                    {
                        File.Copy(_tempFilePath, _originalFilePath, true);
                        OnStatusChanged("원본 파일이 업데이트되었습니다.");
                        FileSaved?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries == 0) throw;
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"저장 오류: {ex.Message}");
            }
        }

        private async void OnProcessExited(object? sender, EventArgs e)
        {
            OnStatusChanged("프로그램이 종료되었습니다.");
            
            // Final save if modified
            if (_isModified)
            {
                _pendingSaveTask = SaveToOriginalAsync();
                await _pendingSaveTask;
            }
            
            // Notify that the process has exited
            ProcessExited?.Invoke(this, EventArgs.Empty);
        }

        private string? GetActualDefaultApplication(string extension)
        {
            try
            {
                string currentExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                
                // First, check custom user-configured application
                string? customApp = ApplicationSettings.GetApplicationPath(extension);
                if (customApp != null && !customApp.Equals(currentExePath, StringComparison.OrdinalIgnoreCase))
                {
                    OnStatusChanged($"사용자 지정 응용 프로그램 사용: {System.IO.Path.GetFileName(customApp)}");
                    return customApp;
                }
                
                // Second, check HKEY_CURRENT_USER for user-specific associations
                string? progId = GetProgIdFromRegistry(Registry.CurrentUser, extension);
                if (progId != null)
                {
                    string? appPath = GetApplicationFromProgId(Registry.CurrentUser, progId);
                    if (appPath != null && !appPath.Equals(currentExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return appPath;
                    }
                }
                
                // Then, check HKEY_LOCAL_MACHINE for system-wide associations
                progId = GetProgIdFromRegistry(Registry.LocalMachine, extension);
                if (progId != null)
                {
                    string? appPath = GetApplicationFromProgId(Registry.LocalMachine, progId);
                    if (appPath != null && !appPath.Equals(currentExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return appPath;
                    }
                }
                
                // Check for common applications based on extension
                return GetCommonApplicationForExtension(extension);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"기본 응용 프로그램 찾기 오류: {ex.Message}");
                return null;
            }
        }

        private string? GetProgIdFromRegistry(RegistryKey rootKey, string extension)
        {
            try
            {
                using var extKey = rootKey.OpenSubKey($@"Software\Classes\{extension}");
                if (extKey != null)
                {
                    var progId = extKey.GetValue("")?.ToString();
                    if (!string.IsNullOrEmpty(progId) && !progId.StartsWith("UnlockOpenFile"))
                    {
                        return progId;
                    }
                }
            }
            catch { }
            return null;
        }

        private string? GetApplicationFromProgId(RegistryKey rootKey, string progId)
        {
            try
            {
                using var commandKey = rootKey.OpenSubKey($@"Software\Classes\{progId}\shell\open\command");
                if (commandKey != null)
                {
                    var command = commandKey.GetValue("")?.ToString();
                    if (!string.IsNullOrEmpty(command))
                    {
                        // Extract the executable path from the command
                        // Command is usually in format: "path\to\app.exe" "%1"
                        int firstQuote = command.IndexOf('"');
                        if (firstQuote >= 0)
                        {
                            int secondQuote = command.IndexOf('"', firstQuote + 1);
                            if (secondQuote > firstQuote)
                            {
                                string exePath = command.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                                // Validate that the executable exists before returning
                                if (File.Exists(exePath))
                                {
                                    return exePath;
                                }
                            }
                        }
                        else
                        {
                            // No quotes, try to get the first part before a space
                            var parts = command.Split(' ');
                            if (parts.Length > 0 && File.Exists(parts[0]))
                            {
                                return parts[0];
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string? GetCommonApplicationForExtension(string extension)
        {
            // Common application paths for well-known extensions
            switch (extension.ToLowerInvariant())
            {
                case ".xlsx":
                case ".xls":
                    // Try to find Excel
                    var excelPaths = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Program Files\Microsoft Office\Office16\EXCEL.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\Office16\EXCEL.EXE"
                    };
                    foreach (var path in excelPaths)
                    {
                        if (File.Exists(path))
                            return path;
                    }
                    break;
                    
                case ".csv":
                    // For CSV, prefer Excel if available, otherwise use notepad
                    var csvApps = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Windows\System32\notepad.exe"
                    };
                    foreach (var path in csvApps)
                    {
                        if (File.Exists(path))
                            return path;
                    }
                    break;
            }
            
            return null;
        }

        public void Cleanup()
        {
            try
            {
                // Wait for any pending save operation to complete
                if (_pendingSaveTask != null && !_pendingSaveTask.IsCompleted)
                {
                    OnStatusChanged("저장 작업 완료 대기 중...");
                    _pendingSaveTask.Wait(TimeSpan.FromSeconds(10)); // Wait up to 10 seconds
                }
                
                _fileWatcher?.Dispose();
                
                if (File.Exists(_tempFilePath))
                {
                    File.Delete(_tempFilePath);
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"정리 오류: {ex.Message}");
            }
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }
    }
}
