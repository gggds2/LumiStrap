using System.Diagnostics;
using Bloxstrap.Models.APIs.GitHub;
using Bloxstrap.UI;

namespace Bloxstrap
{
    public static class LumiUpdateCenter
    {
        public static async Task<GithubRelease?> FindAvailableUpdateAsync()
        {
            GithubRelease? release = await App.GetLatestRelease();
            if (release?.Assets is null || release.Assets.Count == 0 || String.IsNullOrWhiteSpace(release.TagName))
                return null;

            return Utilities.CompareVersions(App.Version, release.TagName) == VersionComparison.LessThan
                ? release
                : null;
        }

        public static async Task<bool> DownloadAndInstallAsync(GithubRelease release)
        {
            var asset = release.Assets?.FirstOrDefault(item =>
                item.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && item.Name.StartsWith(App.ProjectName, StringComparison.OrdinalIgnoreCase));

            asset ??= release.Assets?.FirstOrDefault(item => item.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (asset is null || String.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
                throw new InvalidDataException("La versión no incluye un instalador ejecutable.");

            if (!Frontend.ShowUpdateReviewDialog(App.Version, release.TagName, release.Name, release.Body))
                return false;

            try
            {
                ConfigurationProfile backup = ConfigurationProfileManager.SaveUpdateBackup(release.TagName);
                App.Logger.WriteLine("LumiUpdateCenter", $"Saved pre-update configuration profile '{backup.Name}'");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("LumiUpdateCenter::SaveBackup", ex);
                Frontend.ShowMessageBox(String.Format(Strings.Dialog_UpdateReview_BackupFailed, ex.Message),
                    System.Windows.MessageBoxImage.Error);
                return false;
            }

            Directory.CreateDirectory(Paths.TempUpdates);
            string downloadPath = Path.Combine(Paths.TempUpdates, "LumiStrap-update.exe");
            string partialPath = downloadPath + ".download";
            App.Logger.WriteLine("LumiUpdateCenter", $"Downloading release {release.TagName}");

            try
            {
                using var response = await App.HttpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using (var source = await response.Content.ReadAsStreamAsync())
                await using (var destination = new FileStream(partialPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await source.CopyToAsync(destination);

                File.Move(partialPath, downloadPath, true);

                ProcessStartInfo startInfo = new() { FileName = downloadPath };
                startInfo.ArgumentList.Add("-upgrade");
                foreach (string arg in App.LaunchSettings.Args)
                {
                    if (!String.Equals(arg, "-upgrade", StringComparison.OrdinalIgnoreCase))
                        startInfo.ArgumentList.Add(arg);
                }

                App.Settings.Save();
                new InterProcessLock("AutoUpdater");
                Process.Start(startInfo);
                App.Terminate();
                return true;
            }
            catch
            {
                if (File.Exists(partialPath))
                    File.Delete(partialPath);
                throw;
            }
        }
    }
}
