using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public event EventHandler<string>? StatusChanged;
        public event EventHandler? FileModified;
        public event EventHandler? FileSaved;

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
                _openedProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = _tempFilePath,
                    UseShellExecute = true
                });

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
                    
                    // Save back to original
                    await SaveToOriginalAsync();
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
                await SaveToOriginalAsync();
            }
        }

        public void Cleanup()
        {
            try
            {
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
