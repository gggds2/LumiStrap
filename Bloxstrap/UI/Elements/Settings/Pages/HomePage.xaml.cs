using System.Windows;
using Wpf.Ui.Mvvm.Contracts;
using Bloxstrap.Models.APIs.GitHub;
using Bloxstrap.UI.Elements.Dialogs;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class HomePage
    {
        private bool _checkedForUpdates;
        private bool _checkingForUpdates;
        private GithubRelease? _availableUpdate;

        public HomePage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_checkedForUpdates)
                return;

            _checkedForUpdates = true;
            await CheckForUpdatesAsync();
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

        private async Task CheckForUpdatesAsync()
        {
            if (_checkingForUpdates)
                return;

            _checkingForUpdates = true;
            CheckUpdatesButton.IsEnabled = false;
            UpdateStatusText.Text = "Buscando una versión nueva de LumiStrap…";
            AvailableUpdatePanel.Visibility = Visibility.Collapsed;
            _availableUpdate = null;

            try
            {
                GithubRelease? release = await App.GetLatestRelease();
                if (release is null)
                {
                    UpdateStatusText.Text = "No se pudo consultar GitHub. Comprueba tu conexión e inténtalo de nuevo.";
                }
                else if (Utilities.CompareVersions(App.Version, release.TagName) == VersionComparison.LessThan
                    && release.Assets?.Any(asset => asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) == true)
                {
                    _availableUpdate = release;
                    UpdateVersionText.Text = $"Disponible: {release.Name} ({release.TagName})";
                    UpdateNotesText.Text = UpdateReviewDialog.FormatReleaseNotes(release.Body);
                    UpdateStatusText.Text = "Hay una versión nueva. Revisa sus novedades y decide si quieres instalarla.";
                    AvailableUpdatePanel.Visibility = Visibility.Visible;
                }
                else
                {
                    UpdateStatusText.Text = $"LumiStrap está actualizado. Versión instalada: {App.Version}.";
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("HomePage::CheckForUpdatesAsync", ex);
                UpdateStatusText.Text = "No se pudo consultar GitHub. Comprueba tu conexión e inténtalo de nuevo.";
            }
            finally
            {
                _checkingForUpdates = false;
                CheckUpdatesButton.IsEnabled = true;
            }
        }

        private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_availableUpdate is null)
                return;

            InstallUpdateButton.IsEnabled = false;
            CheckUpdatesButton.IsEnabled = false;
            UpdateStatusText.Text = "Preparando la actualización…";

            try
            {
                bool started = await LumiUpdateCenter.DownloadAndInstallAsync(_availableUpdate);
                if (!started)
                {
                    UpdateStatusText.Text = "La actualización quedó pendiente. Puedes instalarla cuando quieras desde aquí.";
                    InstallUpdateButton.IsEnabled = true;
                    CheckUpdatesButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("HomePage::InstallUpdate", ex);
                UpdateStatusText.Text = $"No se pudo instalar la actualización: {ex.Message}";
                InstallUpdateButton.IsEnabled = true;
                CheckUpdatesButton.IsEnabled = true;
            }
        }

        private void NavigateToSection(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string pageTag }
                && Window.GetWindow(this) is INavigationWindow window)
            {
                window.GetNavigation().Navigate(pageTag);
            }
        }
    }
}
