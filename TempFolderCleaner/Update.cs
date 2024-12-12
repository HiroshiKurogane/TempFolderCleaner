using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace UpdateChecker
{
    public class GitHubReleaseChecker
    {
        private const string GitHubRepoUrl = "https://api.github.com/repos/HiroshiKurogane/TempFolderCleaner/releases/latest";
        private const string CurrentVersion = "1.0.1";  // Your current version here
        private const string CurrentExePath = "TempFolderCleaner.exe";  // Path of the current executable
        private const string TempDownloadPath = "TempFolderCleaner_New.exe"; // Temporary downloaded file

        public static async Task Main(string[] args)
        {
            // Start checking for updates
            await CheckAndDownloadLatestRelease();
        }

        public static async Task CheckAndDownloadLatestRelease()
        {
            using (HttpClient client = new HttpClient())
            {
                // Set the GitHub user agent header to avoid 403 errors
                client.DefaultRequestHeaders.Add("User-Agent", "C#App");

                try
                {
                    // Notify user that the update check is starting
                    MessageBox.Show("Checking for updates...");

                    Console.WriteLine($"Requesting URL: {GitHubRepoUrl}");

                    // Get the latest release info from GitHub API
                    HttpResponseMessage response = await client.GetAsync(GitHubRepoUrl);
                    Console.WriteLine($"Response Status: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Error: {response.ReasonPhrase}");
                        return;
                    }

                    string responseData = await response.Content.ReadAsStringAsync();
                    dynamic releaseData = JsonConvert.DeserializeObject(responseData);

                    // Get the latest release version and URL
                    string latestVersion = releaseData.tag_name;
                    string downloadUrl = null;

                    // Look for TempFolderCleaner.exe in the assets
                    foreach (var asset in releaseData.assets)
                    {
                        string assetName = asset.name.ToString();
                        if (assetName.Equals("TempFolderCleaner.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.browser_download_url;
                            break;
                        }
                    }

                    if (downloadUrl == null)
                    {
                        MessageBox.Show("Updated version was not found in the release assets.");
                        return;
                    }

                    // Remove the leading "v" from the version if it exists
                    latestVersion = latestVersion.TrimStart('v');

                    // Compare versions (simple version comparison)
                    if (CompareVersions(latestVersion, CurrentVersion) > 0)
                    {
                        DialogResult result = MessageBox.Show("A new version is available. Do you want to update?", "Update Available!", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            // Notify user that the download is starting
                            MessageBox.Show("Downloading update...");

                            // Download the update to a temporary location
                            await DownloadFile(downloadUrl, TempDownloadPath);

                            // Close the current application before replacing it
                            CloseCurrentApplication();

                            // Launch the new version of the application
                            Process.Start(TempDownloadPath);

                            // Exit the current process to allow the update to take over
                            Application.Exit();
                        }
                    }
                    else
                    {
                        MessageBox.Show("You already have the latest version.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        public static int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            for (int i = 0; i < Math.Min(v1Parts.Length, v2Parts.Length); i++)
            {
                int comparison = int.Parse(v1Parts[i]).CompareTo(int.Parse(v2Parts[i]));
                if (comparison != 0) return comparison;
            }

            return v1Parts.Length.CompareTo(v2Parts.Length); // If the versions are the same up to the minimum length, compare by length
        }

        public static async Task DownloadFile(string url, string fileName)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    // Read the stream and write it to the file
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }
            }
        }

        public static void CloseCurrentApplication()
        {
            // Close the current application to allow the update to replace the exe
            Process.GetCurrentProcess().Kill();
        }
    }
}
