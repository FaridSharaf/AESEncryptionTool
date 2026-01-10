using System.Windows;
using AESCryptoTool.Models;

namespace AESCryptoTool.Views
{
    public partial class SettingsWindow : Window
    {
        public AppSettings Settings { get; private set; }

        public SettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();
            Settings = new AppSettings
            {
                AutoCopy = currentSettings.AutoCopy,
                AutoDetect = currentSettings.AutoDetect,
                RecentItemsCount = currentSettings.RecentItemsCount,
                MaxHistoryItems = currentSettings.MaxHistoryItems,
                MaxBookmarkItems = currentSettings.MaxBookmarkItems,
                Theme = currentSettings.Theme,
                WindowWidth = currentSettings.WindowWidth,
                WindowHeight = currentSettings.WindowHeight,
                WindowLeft = currentSettings.WindowLeft,
                WindowTop = currentSettings.WindowTop,
                IsMaximized = currentSettings.IsMaximized
            };

            AutoCopyCheckBox.IsChecked = Settings.AutoCopy;
            AutoDetectCheckBox.IsChecked = Settings.AutoDetect;
            RecentItemsCountTextBox.Text = Settings.RecentItemsCount.ToString();
            MaxHistoryItemsTextBox.Text = Settings.MaxHistoryItems.ToString();
            MaxBookmarkItemsTextBox.Text = Settings.MaxBookmarkItems.ToString();
            
            foreach (System.Windows.Controls.ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Content.ToString() == Settings.Theme)
                {
                    item.IsSelected = true;
                    break;
                }
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
                MessageBox.Show("Recent items count must be a positive number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (int.TryParse(MaxHistoryItemsTextBox.Text, out int maxHistory) && maxHistory > 0)
            {
                Settings.MaxHistoryItems = maxHistory;
            }
            else
            {
                MessageBox.Show("Max History must be a positive number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (int.TryParse(MaxBookmarkItemsTextBox.Text, out int maxBookmarks) && maxBookmarks > 0)
            {
                Settings.MaxBookmarkItems = maxBookmarks;
            }
            else
            {
                MessageBox.Show("Max Bookmarks must be a positive number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ThemeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedTheme)
            {
                Settings.Theme = selectedTheme.Content.ToString() ?? "Light";
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private const int MaxAllowedLimit = 1000;

        private void MaxHistoryItemsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(MaxHistoryItemsTextBox.Text, out int value) && value > MaxAllowedLimit)
            {
                MaxHistoryItemsTextBox.Text = MaxAllowedLimit.ToString();
                MaxHistoryItemsTextBox.CaretIndex = MaxHistoryItemsTextBox.Text.Length;
            }
        }

        private void MaxBookmarkItemsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(MaxBookmarkItemsTextBox.Text, out int value) && value > MaxAllowedLimit)
            {
                MaxBookmarkItemsTextBox.Text = MaxAllowedLimit.ToString();
                MaxBookmarkItemsTextBox.CaretIndex = MaxBookmarkItemsTextBox.Text.Length;
            }
        }
    }
}



