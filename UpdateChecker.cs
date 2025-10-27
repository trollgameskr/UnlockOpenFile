using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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
                    return new UpdateInfo
                    {
                        IsUpdateAvailable = true,
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        ReleaseUrl = release.html_url ?? GithubRepoUrl,
                        ReleaseNotes = release.body ?? "",
                        ReleaseName = release.name ?? $"Version {latestVersion}",
                        PublishedAt = release.published_at
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

        private class GitHubRelease
        {
            public string? tag_name { get; set; }
            public string? name { get; set; }
            public string? html_url { get; set; }
            public string? body { get; set; }
            public DateTime published_at { get; set; }
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
    }
}
