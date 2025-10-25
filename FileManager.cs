using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace UnlockOpenFile
{
    public class FileManager
    {
        // Delay constants for file change detection
        // FileChangeDebounceDelayMs: Short delay to coalesce multiple rapid events from a single save operation
        // - Most editors fire multiple Changed events in quick succession
        // - 50ms is sufficient to group these while remaining responsive
        private const int FileChangeDebounceDelayMs = 50;
        
        // AtomicSaveSettleDelayMs: Additional delay for atomic save operations (LibreOffice, etc.)
        // - Atomic saves involve delete+rename operations that need time to complete
        // - 100ms ensures the file system has fully updated before we read the file
        private const int AtomicSaveSettleDelayMs = 100;
        
        private readonly string _originalFilePath;
        private readonly string _tempFilePath;
        private FileSystemWatcher? _fileWatcher;
        private DateTime _lastModified;
        private bool _isModified = false;
        private Process? _openedProcess;
        private Task? _pendingSaveTask;
        private DateTime _fileOpenedTime;
        private System.Threading.Timer? _fileMonitorTimer;

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

        public bool IsFileStillInUse()
        {
            // Check if temp file exists
            if (!File.Exists(_tempFilePath))
                return false;
            
            // If we have a pending save task, wait for it to complete first
            // This prevents false positives when our own save operation is accessing the file
            if (_pendingSaveTask != null && !_pendingSaveTask.IsCompleted)
            {
                try
                {
                    // Wait up to 2 seconds for save to complete
                    // Most saves complete in < 1 second, so this is generous
                    _pendingSaveTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    // Ignore timeout or other errors
                }
            }
            
            // Check if file is locked - retry a few times in case of transient locks
            for (int i = 0; i < 3; i++)
            {
                if (!IsFileLocked(_tempFilePath))
                {
                    // File is not locked
                    // Check if it hasn't been modified recently (within last 2 seconds)
                    // If it was just modified, the editor might still be saving
                    try
                    {
                        var lastWrite = File.GetLastWriteTime(_tempFilePath);
                        var timeSinceModified = DateTime.Now - lastWrite;
                        if (timeSinceModified.TotalSeconds > 2)
                        {
                            // File hasn't been modified recently and is not locked
                            // It's safe to consider it not in use
                            return false;
                        }
                    }
                    catch
                    {
                        // If we can't get last write time, assume file is not in use
                        return false;
                    }
                }
                
                // File is locked or was recently modified, wait a bit and retry
                if (i < 2)
                {
                    System.Threading.Thread.Sleep(300);
                }
            }
            
            // After retries, file is still locked - consider it in use
            return true;
        }

        public Task<bool> OpenFileAsync()
        {
            try
            {
                // Copy the original file to temp location
                OnStatusChanged("원본 파일 복사 중...");
                File.Copy(_originalFilePath, _tempFilePath, true);
                _lastModified = File.GetLastWriteTime(_tempFilePath);
                _fileOpenedTime = DateTime.Now;
                
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
                
                // Monitor process exit, but handle cases where the process exits immediately
                // (e.g., single-instance applications like Excel)
                if (_openedProcess != null && !_openedProcess.HasExited)
                {
                    _openedProcess.EnableRaisingEvents = true;
                    _openedProcess.Exited += OnProcessExited;
                }
                else
                {
                    // Process already exited or wasn't tracked - this is common with
                    // single-instance apps. Keep monitoring via FileSystemWatcher.
                    OnStatusChanged("프로세스 모니터링을 사용할 수 없습니다. 파일 변경 모니터링만 사용합니다.");
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
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.EnableRaisingEvents = true;
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Log the type of change for debugging
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    OnStatusChanged($"파일이 생성되었습니다: {e.Name} (LibreOffice 원자적 저장일 수 있음)");
                }
                
                // Minimal debounce to avoid multiple rapid-fire events from the same save operation
                await Task.Delay(FileChangeDebounceDelayMs);
                
                // Process the file change
                await OnFileChangedInternal();
            }
            catch (Exception ex)
            {
                OnStatusChanged($"파일 변경 감지 오류: {ex.Message}");
            }
        }

        private async void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // LibreOffice and other editors may use atomic save by renaming a temp file
                // If our temp file was the target of a rename, treat it as a change
                if (e.Name == Path.GetFileName(_tempFilePath))
                {
                    OnStatusChanged("파일이 교체되었습니다 (LibreOffice 원자적 저장). 변경 사항을 저장합니다.");
                    
                    // Extra delay to ensure file is fully written after atomic save
                    await Task.Delay(AtomicSaveSettleDelayMs);
                    
                    // Treat rename as a file change
                    await OnFileChangedInternal();
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"파일 이름 변경 감지 오류: {ex.Message}");
            }
        }

        private async Task OnFileChangedInternal()
        {
            try
            {
                if (!File.Exists(_tempFilePath))
                {
                    return;
                }
                
                var currentModified = File.GetLastWriteTime(_tempFilePath);
                if (currentModified > _lastModified)
                {
                    _lastModified = currentModified;
                    _isModified = true;
                    FileModified?.Invoke(this, EventArgs.Empty);
                    
                    // Save back to original immediately and track the task
                    _pendingSaveTask = SaveToOriginalAsync();
                    await _pendingSaveTask;
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"파일 변경 처리 오류: {ex.Message}");
            }
        }

        private async Task SaveToOriginalAsync()
        {
            try
            {
                OnStatusChanged("변경 사항을 원본에 즉시 저장 중...");
                
                // Retry logic for locked files - start immediately without delay
                int retries = 5;
                while (retries > 0)
                {
                    try
                    {
                        File.Copy(_tempFilePath, _originalFilePath, true);
                        OnStatusChanged("원본 파일이 즉시 업데이트되었습니다.");
                        FileSaved?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries == 0) throw;
                        // Only wait if we need to retry due to a lock
                        await Task.Delay(200);
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
            // Check if process exited very quickly (within 3 seconds of opening)
            // This usually means it was a launcher process for a single-instance app
            var timeSinceOpen = DateTime.Now - _fileOpenedTime;
            if (timeSinceOpen.TotalSeconds < 3)
            {
                OnStatusChanged("프로세스가 즉시 종료되었습니다. 단일 인스턴스 응용 프로그램일 수 있습니다.");
                OnStatusChanged("파일이 계속 열려 있습니다. 수동으로 닫거나 임시 파일이 삭제될 때까지 모니터링합니다.");
                
                // Start a timer to periodically check if the temp file still exists
                StartFileMonitoring();
                return;
            }
            
            OnStatusChanged("프로그램이 종료되었습니다.");
            
            // Check for final modifications before saving
            try
            {
                if (File.Exists(_tempFilePath))
                {
                    var currentModified = File.GetLastWriteTime(_tempFilePath);
                    if (currentModified > _lastModified)
                    {
                        // File was modified but FileSystemWatcher might not have fired yet
                        _isModified = true;
                        _lastModified = currentModified;
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"최종 수정 확인 오류: {ex.Message}");
            }
            
            // Final save if modified
            if (_isModified)
            {
                _pendingSaveTask = SaveToOriginalAsync();
                await _pendingSaveTask;
            }
            
            // Notify that the process has exited
            ProcessExited?.Invoke(this, EventArgs.Empty);
        }

        private void StartFileMonitoring()
        {
            // Check every 2 seconds if the temp file is still being used
            _fileMonitorTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    if (!File.Exists(_tempFilePath))
                    {
                        // File was deleted, user is done
                        OnStatusChanged("임시 파일이 삭제되었습니다. 파일을 닫습니다.");
                        StopFileMonitoring();
                        
                        // Final save if modified
                        if (_isModified)
                        {
                            _pendingSaveTask = SaveToOriginalAsync();
                            await _pendingSaveTask;
                        }
                        
                        ProcessExited?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                    
                    // Check if file is still locked (being used by another process)
                    if (!IsFileLocked(_tempFilePath))
                    {
                        // Check for any final modifications before considering the file closed
                        try
                        {
                            var currentModified = File.GetLastWriteTime(_tempFilePath);
                            if (currentModified > _lastModified)
                            {
                                // File was modified, update tracking
                                _isModified = true;
                                _lastModified = currentModified;
                                OnStatusChanged("최종 변경 사항 감지됨.");
                            }
                        }
                        catch (Exception ex)
                        {
                            OnStatusChanged($"최종 수정 확인 오류: {ex.Message}");
                        }
                        
                        // File exists but is not locked - check if it hasn't been modified for a while
                        var timeSinceLastModify = DateTime.Now - _lastModified;
                        if (timeSinceLastModify.TotalSeconds > 10)
                        {
                            // File hasn't been modified in 10 seconds and isn't locked
                            // User probably closed the editor
                            OnStatusChanged("파일이 10초 이상 수정되지 않았고 잠금이 해제되었습니다. 파일을 닫습니다.");
                            StopFileMonitoring();
                            
                            // Final save if modified
                            if (_isModified)
                            {
                                _pendingSaveTask = SaveToOriginalAsync();
                                await _pendingSaveTask;
                            }
                            
                            ProcessExited?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"파일 모니터링 오류: {ex.Message}");
                }
            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        private void StopFileMonitoring()
        {
            _fileMonitorTimer?.Dispose();
            _fileMonitorTimer = null;
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return false;
            }
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
                    // Try to find Excel, then LibreOffice Calc
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
                    
                    // Try LibreOffice Calc as fallback
                    var calcPaths = new[]
                    {
                        @"C:\Program Files\LibreOffice\program\scalc.exe",
                        @"C:\Program Files (x86)\LibreOffice\program\scalc.exe"
                    };
                    foreach (var path in calcPaths)
                    {
                        if (File.Exists(path))
                            return path;
                    }
                    break;
                    
                case ".csv":
                    // For CSV, prefer Excel if available, then LibreOffice Calc, otherwise use notepad
                    var csvApps = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Program Files\LibreOffice\program\scalc.exe",
                        @"C:\Program Files (x86)\LibreOffice\program\scalc.exe",
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
                // Stop file monitoring timer
                StopFileMonitoring();
                
                // Wait for any pending save operation to complete
                if (_pendingSaveTask != null && !_pendingSaveTask.IsCompleted)
                {
                    OnStatusChanged("저장 작업 완료 대기 중...");
                    _pendingSaveTask.Wait(TimeSpan.FromSeconds(10)); // Wait up to 10 seconds
                }
                
                _fileWatcher?.Dispose();
                
                // Perform a final save check before cleanup
                if (File.Exists(_tempFilePath))
                {
                    try
                    {
                        var currentModified = File.GetLastWriteTime(_tempFilePath);
                        if (currentModified > _lastModified)
                        {
                            // File was modified but not yet saved
                            OnStatusChanged("최종 변경 사항 감지, 원본에 저장 중...");
                            _pendingSaveTask = SaveToOriginalAsync();
                            _pendingSaveTask.Wait(TimeSpan.FromSeconds(10));
                        }
                    }
                    catch (Exception ex)
                    {
                        OnStatusChanged($"최종 저장 확인 오류: {ex.Message}");
                    }
                    
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
