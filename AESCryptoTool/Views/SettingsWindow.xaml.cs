using System.Windows;
using System.Windows.Controls;
using AESCryptoTool.Models;
using AESCryptoTool.Services;
using System.Windows.Media;

namespace AESCryptoTool.Views
{
    using Color = System.Windows.Media.Color;
    using ColorConverter = System.Windows.Media.ColorConverter;
    
    public partial class SettingsWindow : Window
    {
        public AppSettings Settings { get; private set; }
        private readonly MainWindow? _mainWindow;
        private bool _isInitializing = true;
        private string _originalTheme;

        public SettingsWindow(AppSettings currentSettings, MainWindow? mainWindow = null)
        {
            InitializeComponent();
            
            // Register window command bindings for custom title bar buttons
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) => {
                if (this.WindowState == WindowState.Maximized)
                    SystemCommands.RestoreWindow(this);
                else
                    SystemCommands.MaximizeWindow(this);
            }));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.RestoreWindowCommand, (s, e) => SystemCommands.RestoreWindow(this)));
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
            
            _mainWindow = mainWindow;
            _originalTheme = currentSettings.Theme;
            
            Settings = new AppSettings
            {
                AutoCopy = currentSettings.AutoCopy,
                AutoDetect = currentSettings.AutoDetect,
                RecentItemsCount = currentSettings.RecentItemsCount,
                MaxHistoryItems = currentSettings.MaxHistoryItems,
                MaxBookmarkItems = currentSettings.MaxBookmarkItems,
                Theme = currentSettings.Theme,
                FontFamily = currentSettings.FontFamily,
                WindowWidth = currentSettings.WindowWidth,
                WindowHeight = currentSettings.WindowHeight,
                WindowLeft = currentSettings.WindowLeft,
                WindowTop = currentSettings.WindowTop,
                IsMaximized = currentSettings.IsMaximized,
                MinimizeToTray = currentSettings.MinimizeToTray,
                CloseToTray = currentSettings.CloseToTray,
                AlwaysOnTop = currentSettings.AlwaysOnTop
            };

            AutoCopyCheckBox.IsChecked = Settings.AutoCopy;
            AutoDetectCheckBox.IsChecked = Settings.AutoDetect;
            RecentItemsCountTextBox.Text = Settings.RecentItemsCount.ToString();
            MaxHistoryItemsTextBox.Text = Settings.MaxHistoryItems.ToString();
            MaxBookmarkItemsTextBox.Text = Settings.MaxBookmarkItems.ToString();
            MinimizeToTrayCheckBox.IsChecked = Settings.MinimizeToTray;
            CloseToTrayCheckBox.IsChecked = Settings.CloseToTray;
            AlwaysOnTopCheckBox.IsChecked = Settings.AlwaysOnTop;
            
            // Populate Theme ComboBox
            ThemeComboBox.Items.Clear();
            ComboBoxItem? itemToSelect = null;

            // Light Themes
            ThemeComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = "── Light Themes ──", 
                IsEnabled = false, 
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) 
            });

            foreach (var theme in ThemeManager.GetLightThemes())
            {
                var item = new ComboBoxItem { Content = theme, Tag = theme };
                ThemeComboBox.Items.Add(item);
                if (theme == Settings.Theme) itemToSelect = item;
            }

            // Dark Themes
            ThemeComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = "── Dark Themes ──", 
                IsEnabled = false, 
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) 
            });

            foreach (var theme in ThemeManager.GetDarkThemes())
            {
                var item = new ComboBoxItem { Content = theme, Tag = theme };
                ThemeComboBox.Items.Add(item);
                if (theme == Settings.Theme) itemToSelect = item;
            }

            if (itemToSelect != null)
            {
                ThemeComboBox.SelectedItem = itemToSelect;
            }
            else
            {
                ThemeComboBox.SelectedIndex = 1; // Default to first actual theme if mismatch
            }
            
            // Initialize Font dropdown
            foreach (ComboBoxItem item in FontComboBox.Items)
            {
                if (item.Tag?.ToString() == Settings.FontFamily)
                {
                    FontComboBox.SelectedItem = item;
                    break;
                }
            }
            if (FontComboBox.SelectedItem == null)
                FontComboBox.SelectedIndex = 0;
            
            _isInitializing = false;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string themeName)
            {
                Settings.Theme = themeName;
                // Live preview
                _mainWindow?.ApplyTheme(themeName);
            }
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            
            if (FontComboBox.SelectedItem is ComboBoxItem item && item.Tag is string fontName)
            {
                Settings.FontFamily = fontName;
                // Live preview
                Services.ThemeManager.ApplyFont(fontName);
            }
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                // Prompt user? Or just do it. Service handles dialog.
                // We export both History and Bookmarks by default for "Backup"
                bool result = await Services.ImportExportService.ExportDataAsync(true, true);
                if (result)
                {
                   CustomMessageBox.Show("Data exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
             try 
             {
                 int count = await Services.ImportExportService.ImportDataAsync();
                 if (count > 0)
                 {
                     CustomMessageBox.Show($"Successfully imported {count} items!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                     _mainWindow?.RefreshData();
                 }
                 else if (count == 0)
                 {
                     // Cancelled or empty
                 }
                 else 
                 {
                      CustomMessageBox.Show("Import failed or no valid data found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 }
             }
             catch (Exception ex)
             {
                 CustomMessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoCopy = AutoCopyCheckBox.IsChecked == true;
            Settings.AutoDetect = AutoDetectCheckBox.IsChecked == true;
            
            if (int.TryParse(RecentItemsCountTextBox.Text, out int count) && count > 0)
            {
                Settings.RecentItemsCount = count;
            }
            else
            {
                CustomMessageBox.Show("Recent items count must be a positive number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (int.TryParse(MaxHistoryItemsTextBox.Text, out int maxHistory) && maxHistory > 0)
            {
                Settings.MaxHistoryItems = maxHistory;
            }
            else
            {
                CustomMessageBox.Show("Max History must be a positive number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (int.TryParse(MaxBookmarkItemsTextBox.Text, out int maxBookmarks) && maxBookmarks > 0)
            {
                Settings.MaxBookmarkItems = maxBookmarks;
            }
            else
            {
                CustomMessageBox.Show("Max Bookmarks must be a positive number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // System Tray settings
            Settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
            Settings.CloseToTray = CloseToTrayCheckBox.IsChecked == true;
            Settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;

            // Update minimize button tooltip if setting changed

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Revert theme if changed
            if (Settings.Theme != _originalTheme)
            {
                _mainWindow?.ApplyTheme(_originalTheme);
            }
            DialogResult = false;
            Close();
        }

        private const int MaxAllowedLimit = 1000;

        private void MaxHistoryItemsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MaxHistoryItemsTextBox.Text, out int value) && value > MaxAllowedLimit)
            {
                MaxHistoryItemsTextBox.Text = MaxAllowedLimit.ToString();
                MaxHistoryItemsTextBox.CaretIndex = MaxHistoryItemsTextBox.Text.Length;
            }
        }

        private void MaxBookmarkItemsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MaxBookmarkItemsTextBox.Text, out int value) && value > MaxAllowedLimit)
            {
                MaxBookmarkItemsTextBox.Text = MaxAllowedLimit.ToString();
                MaxBookmarkItemsTextBox.CaretIndex = MaxBookmarkItemsTextBox.Text.Length;
            }
        }
    }
}



