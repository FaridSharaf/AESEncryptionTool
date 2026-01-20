using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace AESCryptoTool.Services
{
    using Color = System.Windows.Media.Color;
    using ColorConverter = System.Windows.Media.ColorConverter;
    using FontFamily = System.Windows.Media.FontFamily;
    using Application = System.Windows.Application;
    public class Theme
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Light" or "Dark"
        
        // Background colors
        public string PrimaryBg { get; set; } = string.Empty;
        public string SecondaryBg { get; set; } = string.Empty;
        public string CardBg { get; set; } = string.Empty;
        public string RowBg { get; set; } = string.Empty;
        public string HeaderBg { get; set; } = string.Empty;
        
        // Accent colors
        public string PrimaryAccent { get; set; } = string.Empty; // Main identity color (for buttons etc)
        public string AccentBlue { get; set; } = string.Empty;
        public string AccentGreen { get; set; } = string.Empty;
        public string AccentPurple { get; set; } = string.Empty;
        public string AccentOrange { get; set; } = string.Empty;
        public string AccentRed { get; set; } = string.Empty;
        public string AccentYellow { get; set; } = string.Empty;
        
        // Text colors
        public string TextPrimary { get; set; } = string.Empty;
        public string TextSecondary { get; set; } = string.Empty;
        public string TextMuted { get; set; } = string.Empty;
        
        // Border & Selection
        public string BorderColor { get; set; } = string.Empty;
        public string SelectionBg { get; set; } = string.Empty;
    }

    public static class ThemeManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AESCryptoTool", "settings.json");

        public static readonly Dictionary<string, Theme> Themes = new()
        {
            // ===== LIGHT THEMES =====
            ["Light Classic"] = new Theme
            {
                Name = "Light Classic",
                Category = "Light",
                PrimaryBg = "#FFFFFF",
                SecondaryBg = "#F8FAFC",
                CardBg = "#F8FAFC",
                RowBg = "#FFFFFF",
                HeaderBg = "#F8FAFC",
                PrimaryAccent = "#2563EB",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#0F172A",
                TextSecondary = "#64748B",
                TextMuted = "#64748B",
                BorderColor = "#E2E8F0",
                SelectionBg = "#E2E8F0"
            },
            ["Light Soft"] = new Theme
            {
                Name = "Light Soft",
                Category = "Light",
                PrimaryBg = "#FFFBF0",
                SecondaryBg = "#F7F3E8",
                CardBg = "#F7F3E8",
                RowBg = "#FFFBF0",
                HeaderBg = "#F7F3E8",
                PrimaryAccent = "#D97706",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#44403C",
                TextSecondary = "#78716C",
                TextMuted = "#78716C",
                BorderColor = "#E7E5E4",
                SelectionBg = "#E7E5E4"
            },
            ["Light Mint"] = new Theme
            {
                Name = "Light Mint",
                Category = "Light",
                PrimaryBg = "#F0FDFA",
                SecondaryBg = "#E0F2F1",
                CardBg = "#E0F2F1",
                RowBg = "#F0FDFA",
                HeaderBg = "#E0F2F1",
                PrimaryAccent = "#10B981",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#134E4A",
                TextSecondary = "#2D6A66",
                TextMuted = "#2D6A66",
                BorderColor = "#CCDEDD",
                SelectionBg = "#CCDEDD"
            },
            ["Light Rose"] = new Theme
            {
                Name = "Light Rose",
                Category = "Light",
                PrimaryBg = "#FFF1F2",
                SecondaryBg = "#FFE4E6",
                CardBg = "#FFE4E6",
                RowBg = "#FFF1F2",
                HeaderBg = "#FFE4E6",
                PrimaryAccent = "#E11D48",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#881337",
                TextSecondary = "#BE123C",
                TextMuted = "#BE123C",
                BorderColor = "#FECDD3",
                SelectionBg = "#FECDD3"
            },
            ["Lavender Mist"] = new Theme
            {
                Name = "Lavender Mist",
                Category = "Light",
                PrimaryBg = "#FAF5FF",
                SecondaryBg = "#F3E8FF",
                CardBg = "#F3E8FF",
                RowBg = "#FAF5FF",
                HeaderBg = "#F3E8FF",
                PrimaryAccent = "#8B5CF6",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#4C1D95",
                TextSecondary = "#7C3AED",
                TextMuted = "#7C3AED",
                BorderColor = "#E9D5FF",
                SelectionBg = "#E9D5FF"
            },
            ["Solar Flare"] = new Theme
            {
                Name = "Solar Flare",
                Category = "Light",
                PrimaryBg = "#FEFCE8",
                SecondaryBg = "#FEF9C3",
                CardBg = "#FEF9C3",
                RowBg = "#FEFCE8",
                HeaderBg = "#FEF9C3",
                PrimaryAccent = "#F59E0B",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#713F12",
                TextSecondary = "#A16207",
                TextMuted = "#A16207",
                BorderColor = "#FEF08A",
                SelectionBg = "#FEF08A"
            },
            ["Nordic Frost"] = new Theme
            {
                Name = "Nordic Frost",
                Category = "Light",
                PrimaryBg = "#ECEFF4",
                SecondaryBg = "#E5E9F0",
                CardBg = "#E5E9F0",
                RowBg = "#ECEFF4",
                HeaderBg = "#E5E9F0",
                PrimaryAccent = "#5E81AC",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#2E3440",
                TextSecondary = "#4C566A",
                TextMuted = "#4C566A",
                BorderColor = "#D8DEE9",
                SelectionBg = "#D8DEE9"
            },
            ["Paper & Ink"] = new Theme
            {
                Name = "Paper & Ink",
                Category = "Light",
                PrimaryBg = "#FFFCF0",
                SecondaryBg = "#F2F0E5",
                CardBg = "#F2F0E5",
                RowBg = "#FFFCF0",
                HeaderBg = "#F2F0E5",
                PrimaryAccent = "#100F0F",
                AccentBlue = "#3B82F6",
                AccentGreen = "#22C55E",
                AccentPurple = "#A855F7",
                AccentOrange = "#F97316",
                AccentRed = "#EF4444",
                AccentYellow = "#EAB308",
                TextPrimary = "#100F0F",
                TextSecondary = "#6F6E69",
                TextMuted = "#6F6E69",
                BorderColor = "#CECDC3",
                SelectionBg = "#CECDC3"
            },

            // ===== DARK THEMES =====
            ["Ocean Dark"] = new Theme
            {
                Name = "Ocean Dark",
                Category = "Dark",
                PrimaryBg = "#0F172A",
                SecondaryBg = "#1E293B",
                CardBg = "#1E293B",
                RowBg = "#0F172A",
                HeaderBg = "#1E293B",
                PrimaryAccent = "#38BDF8",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#818CF8",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#F1F5F9",
                TextSecondary = "#94A3B8",
                TextMuted = "#94A3B8",
                BorderColor = "#334155",
                SelectionBg = "#334155"
            },
            ["Forest Night"] = new Theme
            {
                Name = "Forest Night",
                Category = "Dark",
                PrimaryBg = "#022C22",
                SecondaryBg = "#064E3B",
                CardBg = "#064E3B",
                RowBg = "#022C22",
                HeaderBg = "#064E3B",
                PrimaryAccent = "#4ADE80",
                AccentBlue = "#38BDF8",
                AccentGreen = "#4ADE80",
                AccentPurple = "#A78BFA",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#ECFDF5",
                TextSecondary = "#6EE7B7",
                TextMuted = "#6EE7B7",
                BorderColor = "#065F46",
                SelectionBg = "#065F46"
            },
            ["Midnight Purple"] = new Theme
            {
                Name = "Midnight Purple",
                Category = "Dark",
                PrimaryBg = "#1D1033",
                SecondaryBg = "#2E1065",
                CardBg = "#2E1065",
                RowBg = "#1D1033",
                HeaderBg = "#2E1065",
                PrimaryAccent = "#A78BFA",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#A78BFA",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#F3E8FF",
                TextSecondary = "#D8B4FE",
                TextMuted = "#D8B4FE",
                BorderColor = "#4C1D95",
                SelectionBg = "#4C1D95"
            },
            ["Sunset Warm"] = new Theme
            {
                Name = "Sunset Warm",
                Category = "Dark",
                PrimaryBg = "#1C1917",
                SecondaryBg = "#292524",
                CardBg = "#292524",
                RowBg = "#1C1917",
                HeaderBg = "#292524",
                PrimaryAccent = "#F97316",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#A78BFA",
                AccentOrange = "#F97316",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#FAFAF9",
                TextSecondary = "#A8A29E",
                TextMuted = "#A8A29E",
                BorderColor = "#44403C",
                SelectionBg = "#44403C"
            },
            ["Cyberpunk Neon"] = new Theme
            {
                Name = "Cyberpunk Neon",
                Category = "Dark",
                PrimaryBg = "#050505",
                SecondaryBg = "#121212",
                CardBg = "#121212",
                RowBg = "#050505",
                HeaderBg = "#121212",
                PrimaryAccent = "#00F0FF",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#A78BFA",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#E0E0E0",
                TextSecondary = "#A0A0A0",
                TextMuted = "#A0A0A0",
                BorderColor = "#333333",
                SelectionBg = "#333333"
            },
            ["Royal Gold"] = new Theme
            {
                Name = "Royal Gold",
                Category = "Dark",
                PrimaryBg = "#120C0E",
                SecondaryBg = "#1F1A1C",
                CardBg = "#1F1A1C",
                RowBg = "#120C0E",
                HeaderBg = "#1F1A1C",
                PrimaryAccent = "#D4AF37",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#A78BFA",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#F9F5F1",
                TextSecondary = "#D4C5B5",
                TextMuted = "#D4C5B5",
                BorderColor = "#3E342F",
                SelectionBg = "#3E342F"
            },
            ["Dracula Berry"] = new Theme
            {
                Name = "Dracula Berry",
                Category = "Dark",
                PrimaryBg = "#282A36",
                SecondaryBg = "#44475A",
                CardBg = "#44475A",
                RowBg = "#282A36",
                HeaderBg = "#44475A",
                PrimaryAccent = "#FF79C6",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#A78BFA",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#F8F8F2",
                TextSecondary = "#BFBFBF",
                TextMuted = "#BFBFBF",
                BorderColor = "#6272A4",
                SelectionBg = "#6272A4"
            },
            ["Slate Monolith"] = new Theme
            {
                Name = "Slate Monolith",
                Category = "Dark",
                PrimaryBg = "#111827",
                SecondaryBg = "#1F2937",
                CardBg = "#1F2937",
                RowBg = "#111827",
                HeaderBg = "#1F2937",
                PrimaryAccent = "#E5E7EB",
                AccentBlue = "#38BDF8",
                AccentGreen = "#34D399",
                AccentPurple = "#A78BFA",
                AccentOrange = "#FB923C",
                AccentRed = "#F87171",
                AccentYellow = "#FACC15",
                TextPrimary = "#F9FAFB",
                TextSecondary = "#9CA3AF",
                TextMuted = "#9CA3AF",
                BorderColor = "#374151",
                SelectionBg = "#374151"
            }
        };

        public static string CurrentTheme { get; private set; } = "Light Classic";

        public static void ApplyTheme(string themeName)
        {
            if (!Themes.TryGetValue(themeName, out var theme))
                theme = Themes["Light Classic"];

            CurrentTheme = themeName;
            
            // Sync with WPF-UI Theme
            var wpfTheme = theme.Category == "Dark" 
                ? Wpf.Ui.Appearance.ApplicationTheme.Dark 
                : Wpf.Ui.Appearance.ApplicationTheme.Light;
            
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(wpfTheme);

            // Ensure Application.Current and Resources are available
            if (Application.Current == null)
            {
                Console.WriteLine("Warning: Application.Current is null, theme will be applied when MainWindow loads");
                return;
            }

            var resources = Application.Current.Resources;
            if (resources == null)
            {
                Console.WriteLine("Warning: Application resources not available yet");
                return;
            }

            // Update resource colors (create new brushes if they don't exist)
            SetOrCreateBrush(resources, "PrimaryBg", theme.PrimaryBg);
            SetOrCreateBrush(resources, "SecondaryBg", theme.SecondaryBg);
            SetOrCreateBrush(resources, "CardBg", theme.CardBg);
            SetOrCreateBrush(resources, "RowBg", theme.RowBg);
            SetOrCreateBrush(resources, "HeaderBg", theme.HeaderBg);
            SetOrCreateBrush(resources, "PrimaryAccent", theme.PrimaryAccent);
            SetOrCreateBrush(resources, "AccentBlue", theme.AccentBlue);
            SetOrCreateBrush(resources, "AccentGreen", theme.AccentGreen);
            SetOrCreateBrush(resources, "AccentPurple", theme.AccentPurple);
            SetOrCreateBrush(resources, "AccentOrange", theme.AccentOrange);
            SetOrCreateBrush(resources, "AccentRed", theme.AccentRed);
            SetOrCreateBrush(resources, "AccentYellow", theme.AccentYellow);
            SetOrCreateBrush(resources, "TextPrimary", theme.TextPrimary);
            SetOrCreateBrush(resources, "TextSecondary", theme.TextSecondary);
            SetOrCreateBrush(resources, "TextMuted", theme.TextMuted);
            SetOrCreateBrush(resources, "BorderColor", theme.BorderColor);
            SetOrCreateBrush(resources, "SelectionBg", theme.SelectionBg);
            
            // Also set TextOnAccent based on theme category
            var textOnAccent = theme.Category == "Dark" ? "#FFFFFF" : "#FFFFFF"; // White on dark, white on light accents
            SetOrCreateBrush(resources, "TextOnAccent", textOnAccent);

            SaveSettings(themeName);
        }
        
        private static void SetOrCreateBrush(ResourceDictionary resources, string key, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var brush = new SolidColorBrush(color);
            
            if (resources.Contains(key))
                resources[key] = brush;
            else
                resources.Add(key, brush);
        }

        public static string LoadSavedTheme()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (settings != null && settings.TryGetValue("theme", out var savedTheme))
                    {
                        if (Themes.ContainsKey(savedTheme))
                            return savedTheme;
                    }
                }
            }
            catch { }
            return "Light Classic";
        }

        private static void SaveSettings(string themeName)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var settings = new Dictionary<string, string> { ["theme"] = themeName };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static IEnumerable<string> GetLightThemes() => 
            Themes.Where(t => t.Value.Category == "Light").Select(t => t.Key);

        public static IEnumerable<string> GetDarkThemes() => 
            Themes.Where(t => t.Value.Category == "Dark").Select(t => t.Key);

        public static void ApplyFont(string fontName)
        {
            if (Application.Current == null) return;
            
            var resources = Application.Current.Resources;
            var fontFamily = new FontFamily(fontName);
            
            // Update the ThemedWindow style's FontFamily
            if (resources["ThemedWindow"] is Style themedWindowStyle)
            {
                // Since we can't modify a style that's in use, we update each window directly
                foreach (Window window in Application.Current.Windows)
                {
                    window.FontFamily = fontFamily;
                }
            }
        }
    }
}
