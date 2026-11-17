using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;
using Bloxstrap.Models;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();

        public WpfUiWindow()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            LumiThemePack? lumiTheme = LumiThemeManager.GetSelectedTheme();
            Enums.Theme finalTheme = App.Settings.Prop.Theme.GetFinal();

            _themeService.SetTheme(finalTheme == Enums.Theme.Dark ? ThemeType.Dark : ThemeType.Light);

            System.Windows.Media.Color primaryAccent = LumiThemeManager.ParseColor(lumiTheme?.AccentPrimary, System.Windows.Media.Color.FromRgb(55, 132, 238));
            System.Windows.Media.Color secondaryAccent = LumiThemeManager.ParseColor(lumiTheme?.AccentSecondary, System.Windows.Media.Color.FromRgb(113, 94, 226));
            System.Windows.Media.Color tertiaryAccent = LumiThemeManager.ParseColor(lumiTheme?.AccentTertiary, System.Windows.Media.Color.FromRgb(174, 91, 218));
            Accent.Apply(primaryAccent, primaryAccent, secondaryAccent, tertiaryAccent);

            // there doesn't seem to be a way to query the name for merged dictionaries
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(finalTheme)}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

#if QA_BUILD
            this.BorderBrush = System.Windows.Media.Brushes.Red;
            this.BorderThickness = new Thickness(4);
#endif
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            base.OnSourceInitialized(e);
        }
    }
}
