using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.IO.Compression;
using System.Linq;

namespace UnlockOpenFile
{
    public class UpdateChecker
    {
        private const string GithubApiUrl = "https://api.github.com/repos/trollgameskr/UnlockOpenFile/releases/latest";
        private const string GithubRepoUrl = "https://github.com/trollgameskr/UnlockOpenFile";
        
        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
        }

        public static async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "UnlockOpenFile-UpdateChecker");
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await httpClient.GetStringAsync(GithubApiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.tag_name))
                {
                    return null;
                }

                var currentVersion = GetCurrentVersion();
                var latestVersion = release.tag_name.TrimStart('v');

                if (IsNewerVersion(latestVersion, currentVersion))
                {
                    // Find the framework-dependent build (smaller, faster download)
                    var downloadAsset = release.assets?.FirstOrDefault(a => 
                        a.name != null && 
                        a.name.EndsWith(".zip") && 
                        !a.name.Contains("standalone") &&
                        !a.name.Contains("checksums"));

                    return new UpdateInfo
                    {
                        IsUpdateAvailable = true,
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        ReleaseUrl = release.html_url ?? GithubRepoUrl,
                        ReleaseNotes = release.body ?? "",
                        ReleaseName = release.name ?? $"Version {latestVersion}",
                        PublishedAt = release.published_at,
                        DownloadUrl = downloadAsset?.browser_download_url ?? "",
                        DownloadSize = downloadAsset?.size ?? 0
                    };
                }

                return new UpdateInfo
                {
                    IsUpdateAvailable = false,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = release.html_url ?? GithubRepoUrl
                };
            }
            catch (Exception ex)
            {
                // Log the error for debugging while failing gracefully
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                // Network issues shouldn't crash the app
                return null;
            }
        }

        private static bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = Version.Parse(latestVersion);
                var current = Version.Parse(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        public static void OpenReleaseUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"브라우저를 열 수 없습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
        {
            try
            {
                // Create temp directory for download
                var tempDir = Path.Combine(Path.GetTempPath(), "UnlockOpenFile_Update");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                var zipPath = Path.Combine(tempDir, "update.zip");
                var extractPath = Path.Combine(tempDir, "extracted");

                // Download the update
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "UnlockOpenFile-UpdateChecker");
                    httpClient.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for download

                    using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var buffer = new byte[8192];
                    var bytesRead = 0L;

                    using var fileStream = File.Create(zipPath);
                    using var contentStream = await response.Content.ReadAsStreamAsync();

                    int read;
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;

                        if (totalBytes > 0 && progress != null)
                        {
                            var percentComplete = (int)((bytesRead * 100) / totalBytes);
                            progress.Report(percentComplete);
                        }
                    }
                }

                // Extract the zip
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // Find the new executable
                var newExePath = Path.Combine(extractPath, "UnlockOpenFile.exe");
                if (!File.Exists(newExePath))
                {
                    MessageBox.Show("업데이트 파일에서 실행 파일을 찾을 수 없습니다.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Create an updater batch script
                var currentExePath = Application.ExecutablePath;
                var updateScriptPath = Path.Combine(tempDir, "update.bat");
                
                var batchScript = $@"@echo off
echo 업데이트를 설치하는 중...
timeout /t 2 /nobreak > nul
copy /Y ""{newExePath}"" ""{currentExePath}""
if errorlevel 1 (
    echo 업데이트 설치 실패
    pause
    exit /b 1
)
echo 업데이트가 완료되었습니다. 프로그램을 다시 시작합니다...
start """" ""{currentExePath}""
rd /s /q ""{tempDir}""
exit
";

                File.WriteAllText(updateScriptPath, batchScript);

                // Start the update script and exit the application
                var processInfo = new ProcessStartInfo
                {
                    FileName = updateScriptPath,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(processInfo);
                
                // Exit the current application
                Application.Exit();
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update installation failed: {ex.Message}");
                MessageBox.Show($"업데이트 설치 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private class GitHubRelease
        {
            public string? tag_name { get; set; }
            public string? name { get; set; }
            public string? html_url { get; set; }
            public string? body { get; set; }
            public DateTime published_at { get; set; }
            public GitHubAsset[]? assets { get; set; }
        }

        private class GitHubAsset
        {
            public string? name { get; set; }
            public string? browser_download_url { get; set; }
            public long size { get; set; }
        }
    }

    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string ReleaseName { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public string DownloadUrl { get; set; } = "";
        public long DownloadSize { get; set; }
    }
}
