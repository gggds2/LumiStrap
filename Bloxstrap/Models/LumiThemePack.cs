using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Bloxstrap.Models
{
    public sealed class LumiThemePack
    {
        public string Name { get; set; } = "Lumi Theme";
        public string AccentPrimary { get; set; } = "#3784EE";
        public string AccentSecondary { get; set; } = "#715EE2";
        public string AccentTertiary { get; set; } = "#AE5BDA";
    }

    public static class LumiThemeManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static readonly Regex ColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        public const string DefaultThemeName = "LumiStrap (predeterminado)";

        public static IReadOnlyList<LumiThemePack> BuiltInThemes { get; } = new[]
        {
            new LumiThemePack { Name = "Lumi Aurora", AccentPrimary = "#00C8FF", AccentSecondary = "#7543F5", AccentTertiary = "#E04CFF" },
            new LumiThemePack { Name = "Lumi Ocean", AccentPrimary = "#00D8C5", AccentSecondary = "#0077CC", AccentTertiary = "#69FFF0" },
            new LumiThemePack { Name = "Lumi Sakura", AccentPrimary = "#FF4D9A", AccentSecondary = "#D63B70", AccentTertiary = "#FFB1D2" },
            new LumiThemePack { Name = "Lumi Mint", AccentPrimary = "#00D99B", AccentSecondary = "#2AA879", AccentTertiary = "#9BFFE4" },
            new LumiThemePack { Name = "Lumi Lumen", AccentPrimary = "#286FE5", AccentSecondary = "#6552D5", AccentTertiary = "#B34BC1" }
        };

        public static LumiThemePack? GetSelectedTheme()
        {
            string? selected = App.Settings.Prop.SelectedLumiTheme;
            if (String.IsNullOrWhiteSpace(selected))
                return null;

            const string legacySuffix = " personalizado";
            if (selected.EndsWith(legacySuffix, StringComparison.OrdinalIgnoreCase))
            {
                string originalName = selected[..^legacySuffix.Length];
                LumiThemePack? builtIn = BuiltInThemes.FirstOrDefault(theme => String.Equals(theme.Name, originalName, StringComparison.OrdinalIgnoreCase));
                if (builtIn is not null)
                {
                    try
                    {
                        string legacyPath = Path.Combine(Paths.LumiThemes, selected + ".lumitheme");
                        if (File.Exists(legacyPath))
                        {
                            LumiThemePack legacyPack = Deserialize(File.ReadAllText(legacyPath));
                            legacyPack.Name = builtIn.Name;
                            string overridePath = Path.Combine(Paths.LumiThemes, builtIn.Name + ".lumitheme");
                            Directory.CreateDirectory(Paths.LumiThemes);
                            File.WriteAllText(overridePath, Serialize(legacyPack));
                            App.Settings.Prop.SelectedLumiTheme = builtIn.Name;
                            return legacyPack;
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteException("LumiThemeManager::MigrateLegacyTheme", ex);
                    }
                }
            }

            return GetTheme(selected);
        }

        public static bool IsLegacyBuiltInVariant(string? name)
        {
            const string legacySuffix = " personalizado";
            if (String.IsNullOrWhiteSpace(name) || !name.EndsWith(legacySuffix, StringComparison.OrdinalIgnoreCase))
                return false;

            string originalName = name[..^legacySuffix.Length];
            return BuiltInThemes.Any(theme => String.Equals(theme.Name, originalName, StringComparison.OrdinalIgnoreCase));
        }

        public static LumiThemePack? GetTheme(string? name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return null;

            try
            {
                string root = Path.GetFullPath(Paths.LumiThemes) + Path.DirectorySeparatorChar;
                string path = Path.GetFullPath(Path.Combine(Paths.LumiThemes, name + ".lumitheme"));
                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                    return Deserialize(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("LumiThemeManager::GetTheme", ex);
            }

            if (String.Equals(name, DefaultThemeName, StringComparison.OrdinalIgnoreCase))
                return new LumiThemePack { Name = DefaultThemeName };

            LumiThemePack? builtIn = BuiltInThemes.FirstOrDefault(theme => String.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase));
            if (builtIn is not null)
                return new LumiThemePack
                {
                    Name = builtIn.Name,
                    AccentPrimary = builtIn.AccentPrimary,
                    AccentSecondary = builtIn.AccentSecondary,
                    AccentTertiary = builtIn.AccentTertiary
                };

            return null;
        }

        public static LumiThemePack Deserialize(string json)
        {
            LumiThemePack pack = JsonSerializer.Deserialize<LumiThemePack>(json)
                ?? throw new InvalidDataException("El archivo no contiene un tema Lumi válido.");

            pack.Name = SanitizeName(pack.Name);
            ValidateColor(pack.AccentPrimary, nameof(pack.AccentPrimary));
            ValidateColor(pack.AccentSecondary, nameof(pack.AccentSecondary));
            ValidateColor(pack.AccentTertiary, nameof(pack.AccentTertiary));
            return pack;
        }

        public static string Serialize(LumiThemePack pack) => JsonSerializer.Serialize(pack, JsonOptions);

        public static Color ParseColor(string? value, Color fallback)
        {
            if (String.IsNullOrWhiteSpace(value) || !ColorPattern.IsMatch(value))
                return fallback;

            return (Color)ColorConverter.ConvertFromString(value)!;
        }

        public static string SanitizeName(string? name)
        {
            string cleaned = Regex.Replace(name?.Trim() ?? "", @"[^\p{L}\p{N} _-]", "");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            if (cleaned.Length == 0 || cleaned.Length > 40)
                throw new InvalidDataException("El nombre del tema debe tener entre 1 y 40 caracteres.");
            return cleaned;
        }

        private static void ValidateColor(string? value, string field)
        {
            if (String.IsNullOrWhiteSpace(value) || !ColorPattern.IsMatch(value))
                throw new InvalidDataException($"El color {field} debe usar el formato #RRGGBB.");
        }
    }
}
