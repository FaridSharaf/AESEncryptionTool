using System.Windows;
using ConsoleApp1.Models;

namespace ConsoleApp1.Views
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
    }
}



