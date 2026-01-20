using Wpf.Ui.Appearance;
using System.Windows.Media;

namespace AESCryptoTool.Services
{
    /// <summary>
    /// Simplified theme service that uses WPF-UI's native theming system
    /// </summary>
    public static class ThemeService
    {
        /// <summary>
        /// Apply a theme (Light, Dark, or HighContrast)
        /// </summary>
        public static void ApplyTheme(ApplicationTheme theme)
        {
            ApplicationThemeManager.Apply(theme);
        }

        /// <summary>
        /// Apply theme based on system preference
        /// </summary>
        public static void ApplySystemTheme()
        {
            ApplicationThemeManager.Apply(
                ApplicationTheme.Unknown, // Let WPF-UI detect system theme
                updateAccent: false
            );
        }
        
        /// <summary>
        /// Set the accent color for the application
        /// </summary>
        public static void SetAccentColor(System.Windows.Media.Color color)
        {
            ApplicationAccentColorManager.Apply(color);
        }

        /// <summary>
        /// Get the current theme
        /// </summary>
        public static ApplicationTheme GetCurrentTheme()
        {
            return ApplicationThemeManager.GetAppTheme();
        }
    }
}
