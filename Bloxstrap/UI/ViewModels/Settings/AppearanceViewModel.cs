using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;

using Microsoft.Win32;

using Bloxstrap.UI.Elements.Settings;
using Bloxstrap.UI.Elements.Editor;
using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.Models;
using System.Text.Json;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class AppearanceViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Page _page;

        public ICommand PreviewBootstrapperCommand => new RelayCommand(PreviewBootstrapper);
        public ICommand BrowseCustomIconLocationCommand => new RelayCommand(BrowseCustomIconLocation);

        public ICommand AddCustomThemeCommand => new RelayCommand(AddCustomTheme);
        public ICommand DeleteCustomThemeCommand => new RelayCommand(DeleteCustomTheme);
        public ICommand RenameCustomThemeCommand => new RelayCommand(RenameCustomTheme);
        public ICommand EditCustomThemeCommand => new RelayCommand(EditCustomTheme);
        public ICommand ExportCustomThemeCommand => new RelayCommand(ExportCustomTheme);
        public ICommand SaveLumiThemeCommand => new RelayCommand(SaveLumiTheme);
        public ICommand ImportLumiThemeCommand => new RelayCommand(ImportLumiTheme);
        public ICommand ExportLumiThemeCommand => new RelayCommand(ExportLumiTheme);
        public ICommand ResetLumiThemeCommand => new RelayCommand(ResetLumiTheme);
        public ICommand PickPrimaryColorCommand => new RelayCommand(() => PickLumiThemeColor(nameof(NewLumiThemePrimary)));
        public ICommand PickSecondaryColorCommand => new RelayCommand(() => PickLumiThemeColor(nameof(NewLumiThemeSecondary)));
        public ICommand PickTertiaryColorCommand => new RelayCommand(() => PickLumiThemeColor(nameof(NewLumiThemeTertiary)));

        public ObservableCollection<string> LumiThemeOptions { get; } = new();
        public string DefaultLumiThemeOption => LumiThemeManager.DefaultThemeName;
        public string SelectedLumiTheme
        {
            get => App.Settings.Prop.SelectedLumiTheme ?? DefaultLumiThemeOption;
            set
            {
                App.Settings.Prop.SelectedLumiTheme = value == DefaultLumiThemeOption ? null : value;
                LoadThemeIntoEditor(value);
                if (_page.IsLoaded && Window.GetWindow(_page) is MainWindow mainWindow)
                    mainWindow.ApplyTheme();
                OnPropertyChanged(nameof(SelectedLumiTheme));
            }
        }

        public string NewLumiThemeName { get; set; } = "Mi tema Lumi";
        public string NewLumiThemePrimary { get; set; } = "#3784EE";
        public string NewLumiThemeSecondary { get; set; } = "#715EE2";
        public string NewLumiThemeTertiary { get; set; } = "#AE5BDA";
        private void LoadThemeIntoEditor(string? name)
        {
            LumiThemePack? pack = LumiThemeManager.GetTheme(name);
            if (pack is null)
                pack = new LumiThemePack { Name = "Mi tema Lumi" };

            NewLumiThemeName = pack.Name == DefaultLumiThemeOption ? "Mi tema Lumi" : pack.Name;
            NewLumiThemePrimary = pack.AccentPrimary;
            NewLumiThemeSecondary = pack.AccentSecondary;
            NewLumiThemeTertiary = pack.AccentTertiary;
            OnPropertyChanged(nameof(NewLumiThemeName));
            OnPropertyChanged(nameof(NewLumiThemePrimary));
            OnPropertyChanged(nameof(NewLumiThemeSecondary));
            OnPropertyChanged(nameof(NewLumiThemeTertiary));
        }

        private void PreviewBootstrapper()
        {
            IBootstrapperDialog dialog = App.Settings.Prop.BootstrapperStyle.GetNew();

            if (App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.ByfronDialog)
                dialog.Message = Strings.Bootstrapper_StylePreview_ImageCancel;
            else
                dialog.Message = Strings.Bootstrapper_StylePreview_TextCancel;

            dialog.CancelEnabled = true;
            dialog.ShowBootstrapper();
        }

        private void BrowseCustomIconLocation()
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_IconFiles}|*.ico"
            };

            if (dialog.ShowDialog() != true)
                return;

            CustomIconLocation = dialog.FileName;
            OnPropertyChanged(nameof(CustomIconLocation));
        }

        public AppearanceViewModel(Page page)
        {
            _page = page;

            foreach (var entry in BootstrapperIconEx.Selections)
                Icons.Add(new BootstrapperIconEntry { IconType = entry });

            PopulateCustomThemes();
            PopulateLumiThemes();
        }

        private void PopulateLumiThemes()
        {
            Directory.CreateDirectory(Paths.LumiThemes);
            LumiThemeOptions.Clear();
            LumiThemeOptions.Add(DefaultLumiThemeOption);
            foreach (LumiThemePack pack in LumiThemeManager.BuiltInThemes)
                LumiThemeOptions.Add(pack.Name);

            foreach (string path in Directory.EnumerateFiles(Paths.LumiThemes, "*.lumitheme"))
            {
                try
                {
                    LumiThemePack pack = LumiThemeManager.Deserialize(File.ReadAllText(path));
                    if (LumiThemeManager.IsLegacyBuiltInVariant(pack.Name))
                        continue;

                    if (!LumiThemeOptions.Contains(pack.Name, StringComparer.OrdinalIgnoreCase))
                        LumiThemeOptions.Add(pack.Name);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("AppearanceViewModel::PopulateLumiThemes", ex);
                }
            }

            OnPropertyChanged(nameof(LumiThemeOptions));
            OnPropertyChanged(nameof(SelectedLumiTheme));
        }

        private void PickLumiThemeColor(string propertyName)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            string current = propertyName switch
            {
                nameof(NewLumiThemePrimary) => NewLumiThemePrimary,
                nameof(NewLumiThemeSecondary) => NewLumiThemeSecondary,
                _ => NewLumiThemeTertiary
            };

            try
            {
                dialog.Color = System.Drawing.ColorTranslator.FromHtml(current);
            }
            catch { }

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string value = System.Drawing.ColorTranslator.ToHtml(dialog.Color);
            if (propertyName == nameof(NewLumiThemePrimary)) NewLumiThemePrimary = value;
            else if (propertyName == nameof(NewLumiThemeSecondary)) NewLumiThemeSecondary = value;
            else NewLumiThemeTertiary = value;
            OnPropertyChanged(propertyName);
        }

        private void SaveLumiTheme()
        {
            try
            {
                LumiThemePack pack = LumiThemeManager.Deserialize(JsonSerializer.Serialize(new LumiThemePack
                {
                    Name = NewLumiThemeName,
                    AccentPrimary = NewLumiThemePrimary,
                    AccentSecondary = NewLumiThemeSecondary,
                    AccentTertiary = NewLumiThemeTertiary
                }));

                Directory.CreateDirectory(Paths.LumiThemes);
                string path = Path.Combine(Paths.LumiThemes, pack.Name + ".lumitheme");
                if (File.Exists(path) && Frontend.ShowMessageBox($"¿Reemplazar el tema «{pack.Name}»?", MessageBoxImage.Question, MessageBoxButton.YesNo, MessageBoxResult.No) != MessageBoxResult.Yes)
                    return;

                File.WriteAllText(path, LumiThemeManager.Serialize(pack));
                PopulateLumiThemes();
                SelectedLumiTheme = pack.Name;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::SaveLumiTheme", ex);
                Frontend.ShowMessageBox($"No se pudo guardar el tema: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void ImportLumiTheme()
        {
            var dialog = new OpenFileDialog { Filter = "Temas Lumi|*.lumitheme;*.json|Todos los archivos|*.*" };
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LumiThemePack pack = LumiThemeManager.Deserialize(File.ReadAllText(dialog.FileName));
                Directory.CreateDirectory(Paths.LumiThemes);
                string name = pack.Name;
                string path = Path.Combine(Paths.LumiThemes, name + ".lumitheme");
                for (int suffix = 2; File.Exists(path) || LumiThemeManager.BuiltInThemes.Any(theme => String.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase)); suffix++)
                {
                    string prefix = pack.Name.Length > 34 ? pack.Name[..34].TrimEnd() : pack.Name;
                    name = LumiThemeManager.SanitizeName($"{prefix} {suffix}");
                    path = Path.Combine(Paths.LumiThemes, name + ".lumitheme");
                }

                pack.Name = name;
                File.WriteAllText(path, LumiThemeManager.Serialize(pack));
                PopulateLumiThemes();
                SelectedLumiTheme = name;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::ImportLumiTheme", ex);
                Frontend.ShowMessageBox($"No se pudo importar el tema: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void ExportLumiTheme()
        {
            try
            {
                LumiThemePack? pack = LumiThemeManager.GetTheme(SelectedLumiTheme);
                if (pack is null)
                    throw new InvalidDataException("El tema seleccionado no está disponible.");

                var dialog = new SaveFileDialog { FileName = pack.Name + ".lumitheme", Filter = "Tema Lumi|*.lumitheme" };
                if (dialog.ShowDialog() != true)
                    return;

                File.WriteAllText(dialog.FileName, LumiThemeManager.Serialize(pack));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::ExportLumiTheme", ex);
                Frontend.ShowMessageBox($"No se pudo exportar el tema: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void ResetLumiTheme()
        {
            string? selected = App.Settings.Prop.SelectedLumiTheme;
            bool isBuiltIn = LumiThemeManager.BuiltInThemes.Any(theme => String.Equals(theme.Name, selected, StringComparison.OrdinalIgnoreCase));

            if (isBuiltIn)
            {
                string overridePath = Path.Combine(Paths.LumiThemes, selected + ".lumitheme");
                if (File.Exists(overridePath))
                    File.Delete(overridePath);

                LoadThemeIntoEditor(selected);
                if (_page.IsLoaded && Window.GetWindow(_page) is MainWindow mainWindow)
                    mainWindow.ApplyTheme();
                PopulateLumiThemes();
                return;
            }

            SelectedLumiTheme = DefaultLumiThemeOption;
        }

        public IEnumerable<Theme> Themes { get; } = Enum.GetValues(typeof(Theme)).Cast<Theme>();

        public Theme Theme
        {
            get => App.Settings.Prop.Theme;
            set
            {
                if (App.Settings.Prop.Theme == value)
                    return;

                App.Settings.Prop.Theme = value;
                if (Window.GetWindow(_page) is MainWindow mainWindow)
                {
                    mainWindow.ApplyTheme();
                }
                else if (Application.Current is { } application)
                {
                    // The page can receive its initial binding before it is attached to the window.
                    // Defer the refresh so choosing Light/Dark always updates the open settings window.
                    application.Dispatcher.BeginInvoke(() =>
                    {
                        foreach (Window window in application.Windows)
                        {
                            if (window is MainWindow openMainWindow)
                                openMainWindow.ApplyTheme();
                        }
                    });
                }

                OnPropertyChanged(nameof(Theme));
            }
        }

        public static List<string> Languages => Locale.GetLanguages();

        public string SelectedLanguage 
        { 
            get => Locale.SupportedLocales[App.Settings.Prop.Locale]; 
            set => App.Settings.Prop.Locale = Locale.GetIdentifierFromName(value);
        }

        public IEnumerable<BootstrapperStyle> Dialogs { get; } = BootstrapperStyleEx.Selections;

        public BootstrapperStyle Dialog
        {
            get => App.Settings.Prop.BootstrapperStyle;
            set
            {
                App.Settings.Prop.BootstrapperStyle = value;
                OnPropertyChanged(nameof(CustomThemesExpanded)); // TODO: only fire when needed
            }
        }

        public bool CustomThemesExpanded => App.Settings.Prop.BootstrapperStyle == BootstrapperStyle.CustomDialog;

        public ObservableCollection<BootstrapperIconEntry> Icons { get; set; } = new();

        public BootstrapperIcon Icon
        {
            get => App.Settings.Prop.BootstrapperIcon;
            set => App.Settings.Prop.BootstrapperIcon = value; 
        }

        public string Title
        {
            get => App.Settings.Prop.BootstrapperTitle;
            set => App.Settings.Prop.BootstrapperTitle = value;
        }

        public string CustomIconLocation
        {
            get => App.Settings.Prop.BootstrapperIconCustomLocation;
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (App.Settings.Prop.BootstrapperIcon == BootstrapperIcon.IconCustom)
                        App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconBloxstrap;
                }
                else
                {
                    App.Settings.Prop.BootstrapperIcon = BootstrapperIcon.IconCustom;
                }

                App.Settings.Prop.BootstrapperIconCustomLocation = value;

                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Icons));
            }
        }

        private void DeleteCustomThemeStructure(string name)
        {
            string dir = Path.Combine(Paths.CustomThemes, name);
            Directory.Delete(dir, true);
        }

        private void RenameCustomThemeStructure(string oldName, string newName)
        {
            string oldDir = Path.Combine(Paths.CustomThemes, oldName);
            string newDir = Path.Combine(Paths.CustomThemes, newName);
            Directory.Move(oldDir, newDir);
        }

        private void AddCustomTheme()
        {
            var dialog = new AddCustomThemeDialog();
            dialog.ShowDialog();

            if (dialog.Created)
            {
                CustomThemes.Add(dialog.ThemeName);
                SelectedCustomThemeIndex = CustomThemes.Count - 1;

                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                OnPropertyChanged(nameof(IsCustomThemeSelected));

                if (dialog.OpenEditor)
                    EditCustomTheme();
            }
        }

        private void DeleteCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            try
            {
                DeleteCustomThemeStructure(SelectedCustomTheme);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppearanceViewModel::DeleteCustomTheme", ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_DeleteFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            CustomThemes.Remove(SelectedCustomTheme);

            if (CustomThemes.Any())
            {
                SelectedCustomThemeIndex = CustomThemes.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomThemeIndex));
            }

            OnPropertyChanged(nameof(IsCustomThemeSelected));
        }

        private void RenameCustomTheme()
        {
            const string LOG_IDENT = "AppearanceViewModel::RenameCustomTheme";

            if (SelectedCustomTheme is null || SelectedCustomTheme == SelectedCustomThemeName)
                return;

            if (string.IsNullOrEmpty(SelectedCustomThemeName))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameEmpty, MessageBoxImage.Error);
                return;
            }

            var validationResult = PathValidator.IsFileNameValid(SelectedCustomThemeName);

            if (validationResult != PathValidator.ValidationResult.Ok)
            {
                switch (validationResult)
                {
                    case PathValidator.ValidationResult.IllegalCharacter:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameIllegalCharacters, MessageBoxImage.Error);
                        break;
                    case PathValidator.ValidationResult.ReservedFileName:
                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameReserved, MessageBoxImage.Error);
                        break;
                    default:
                        App.Logger.WriteLine(LOG_IDENT, $"Got unhandled PathValidator::ValidationResult {validationResult}");
                        Debug.Assert(false);

                        Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_Unknown, MessageBoxImage.Error);
                        break;
                }

                return;
            }

            // better to check for the file instead of the directory so broken themes can be overwritten
            string path = Path.Combine(Paths.CustomThemes, SelectedCustomThemeName, "Theme.xml");
            if (File.Exists(path))
            {
                Frontend.ShowMessageBox(Strings.CustomTheme_Add_Errors_NameTaken, MessageBoxImage.Error);
                return;
            }

            try
            {
                RenameCustomThemeStructure(SelectedCustomTheme, SelectedCustomThemeName);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox(string.Format(Strings.Menu_Appearance_CustomThemes_RenameFailed, SelectedCustomTheme, ex.Message), MessageBoxImage.Error);
                return;
            }

            int idx = CustomThemes.IndexOf(SelectedCustomTheme);
            CustomThemes[idx] = SelectedCustomThemeName;

            SelectedCustomThemeIndex = idx;
            OnPropertyChanged(nameof(SelectedCustomThemeIndex));
        }

        private void EditCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            new BootstrapperEditorWindow(SelectedCustomTheme).ShowDialog();
        }

        private void ExportCustomTheme()
        {
            if (SelectedCustomTheme is null)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"{SelectedCustomTheme}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            string themeDir = Path.Combine(Paths.CustomThemes, SelectedCustomTheme);

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            foreach (var filePath in Directory.EnumerateFiles(themeDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = filePath[(themeDir.Length + 1)..];

                var entry = new ZipEntry(relativePath);
                entry.DateTime = DateTime.Now;

                zipStream.PutNextEntry(entry);

                using var fileStream = File.OpenRead(filePath);
                fileStream.CopyTo(zipStream);
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void PopulateCustomThemes()
        {
            string? selected = App.Settings.Prop.SelectedCustomTheme;

            Directory.CreateDirectory(Paths.CustomThemes);

            foreach (string directory in Directory.GetDirectories(Paths.CustomThemes))
            {
                if (!File.Exists(Path.Combine(directory, "Theme.xml")))
                    continue; // missing the main theme file, ignore

                string name = Path.GetFileName(directory);
                CustomThemes.Add(name);
            }

            if (selected != null)
            {
                int idx = CustomThemes.IndexOf(selected);

                if (idx != -1)
                {
                    SelectedCustomThemeIndex = idx;
                    OnPropertyChanged(nameof(SelectedCustomThemeIndex));
                }
                else
                {
                    SelectedCustomTheme = null;
                }
            }
        }

        public string? SelectedCustomTheme
        {
            get => App.Settings.Prop.SelectedCustomTheme;
            set => App.Settings.Prop.SelectedCustomTheme = value;
        }

        public string SelectedCustomThemeName { get; set; } = "";

        public int SelectedCustomThemeIndex { get; set; }

        public ObservableCollection<string> CustomThemes { get; set; } = new();
        public bool IsCustomThemeSelected => SelectedCustomTheme is not null;
    }
}
