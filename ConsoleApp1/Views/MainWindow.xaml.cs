using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ConsoleApp1.Models;
using ConsoleApp1.Services;

namespace ConsoleApp1.Views
{
    /// <summary>
    /// Converter for showing bookmark/tag icon based on status
    /// </summary>
    public class BoolToBookmarkConverter : IValueConverter
    {
        public static readonly BoolToBookmarkConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? "🔖" : "🏷️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "🔖";
        }
    }

    /// <summary>
    /// Converter for showing note (if available) or output in recent items
    /// </summary>
    public class NoteOrOutputConverter : IValueConverter
    {
        public static readonly NoteOrOutputConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HistoryEntry entry)
            {
                return string.IsNullOrWhiteSpace(entry.Note) ? entry.Output : $"📝 {entry.Note}";
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Window
    {
        private AppConfig _config = null!;
        private AppSettings _settings = null!;
        private bool _keyMasked = true;
        private bool _ivMasked = true;
        private HistoryEntry? _currentEncryptEntry;
        private HistoryEntry? _currentDecryptEntry;
        private bool _encryptFavorite = false;
        private bool _decryptFavorite = false;
        // private bool _showingFavoritesOnly = false; // Removed
        
        // Use ObservableCollection to prevent grid resets
        public System.Collections.ObjectModel.ObservableCollection<HistoryEntry> HistoryItems { get; set; } = new();
        public System.Collections.ObjectModel.ObservableCollection<HistoryEntry> BookmarksItems { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            LoadKeys();
            
            // Bind DataGrid to ObservableCollection
            HistoryDataGrid.ItemsSource = HistoryItems;
            
            // Note: BookmarksDataGrid binding happens in XAML or we can set it here if x:Name matches
            // We'll set it here to be safe after component init
            if (this.FindName("BookmarksDataGrid") is DataGrid bookmarksGrid)
            {
                bookmarksGrid.ItemsSource = BookmarksItems;
            }

            // Enforce max limits on startup (separate files)
            HistoryManager.EnforceHistoryLimit(_settings.MaxHistoryItems);
            HistoryManager.EnforceBookmarkLimit(_settings.MaxBookmarkItems);

            LoadHistory();
            SetupKeyboardShortcuts();
            UpdateStatus("✓ Ready - Using double encryption (plaintext keys)");
        }

        private void LoadSettings()
        {
            _settings = ConfigManager.LoadSettings();
            
            if (_settings.WindowWidth > 0) Width = _settings.WindowWidth;
            if (_settings.WindowHeight > 0) Height = _settings.WindowHeight;
            if (_settings.WindowLeft > 0) Left = _settings.WindowLeft;
            if (_settings.WindowTop > 0) Top = _settings.WindowTop;
            if (_settings.IsMaximized) WindowState = WindowState.Maximized;

            EncryptAutoDetectCheckBox.IsChecked = _settings.AutoDetect;
            DecryptAutoDetectCheckBox.IsChecked = _settings.AutoDetect;
        }

        private void LoadKeys()
        {
            _config = ConfigManager.LoadKeys();
            
            // Initial state: fully hidden
            KeyTextBox.Text = new string('•', _config.Key.Length > 0 ? _config.Key.Length : 16);
            IVTextBox.Text = new string('•', _config.IV.Length > 0 ? _config.IV.Length : 16);
        }

        /// <summary>
        /// Masks a value showing only first 2 and last 4 characters
        /// </summary>
        private string MaskValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new string('•', 8);
            
            if (value.Length <= 6)
                return new string('•', value.Length);
            
            // Show first 2 and last 4, mask the rest
            string prefix = value.Substring(0, 2);
            string suffix = value.Substring(value.Length - 4);
            int maskedLength = value.Length - 6;
            return prefix + new string('•', maskedLength) + suffix;
        }

        private void LoadHistory()
        {
            RefreshHistory();
            RefreshRecentItems();
        }

        private void RefreshHistory()
        {
            // Reload all history
            var allHistory = HistoryManager.LoadHistory();
            
            // Update History Items (Main list)
            HistoryItems.Clear();
            foreach (var item in allHistory)
            {
                HistoryItems.Add(item);
            }
            
            // Update Bookmarks Items
            BookmarksItems.Clear();
            var favorites = HistoryManager.GetFavorites();
            foreach (var item in favorites)
            {
                BookmarksItems.Add(item);
            }

            if (HistoryItems.Count > 0)
            {
                HistoryLabel.Text = $"📜 History ({HistoryItems.Count})";
            }
            else
            {
                HistoryLabel.Text = "📜 History";
            }
        }

        private void HistorySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = HistorySearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshHistory();
            }
            else
            {
                var allHistory = HistoryManager.LoadHistory();
                var results = allHistory.Where(entry =>
                    entry.Input.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    entry.Output.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    entry.Note.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                HistoryItems.Clear();
                foreach (var item in results)
                {
                    HistoryItems.Add(item);
                }
            }
        }

        private void BookmarksSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = BookmarksSearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshHistory();
            }
            else
            {
                var allBookmarks = HistoryManager.GetFavorites();
                var results = allBookmarks.Where(entry =>
                    entry.Input.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    entry.Output.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    entry.Note.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                BookmarksItems.Clear();
                foreach (var item in results)
                {
                    BookmarksItems.Add(item);
                }
            }
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL history entries?\nThis action cannot be undone.",
                "Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.ClearHistory();
                RefreshHistory();
                RefreshRecentItems();
                UpdateStatus("🗑️ History cleared");
            }
        }

        private void ClearBookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to remove ALL bookmarks?\nThis action cannot be undone.",
                "Clear Bookmarks",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.ClearBookmarks();
                RefreshHistory();
                RefreshRecentItems();
                UpdateStatus("🗑️ Bookmarks cleared");
            }
        }

        private void RefreshRecentItems()
        {
            var recent = HistoryManager.GetRecentItems(_settings.RecentItemsCount);
            RecentItemsControl.ItemsSource = recent;
        }

        private void SetupKeyboardShortcuts()
        {
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    if (EncryptInputTextBox.IsFocused)
                        EncryptInputTextBox.Clear();
                    else if (DecryptInputTextBox.IsFocused)
                        DecryptInputTextBox.Clear();
                }
            };
        }

        // Key/IV Management
        private void KeyShowButton_Click(object sender, RoutedEventArgs e)
        {
            _keyMasked = !_keyMasked;
            if (_keyMasked)
            {
                KeyShowButton.Content = "👁️";
                KeyTextBox.Text = new string('•', _config.Key.Length > 0 ? _config.Key.Length : 16);
            }
            else
            {
                KeyShowButton.Content = "🙈";
                // Show partial mask only - never full key
                KeyTextBox.Text = MaskValue(_config.Key);
            }
        }

        private void IVShowButton_Click(object sender, RoutedEventArgs e)
        {
            _ivMasked = !_ivMasked;
            if (_ivMasked)
            {
                IVShowButton.Content = "👁️";
                IVTextBox.Text = new string('•', _config.IV.Length > 0 ? _config.IV.Length : 16);
            }
            else
            {
                IVShowButton.Content = "🙈";
                // Show partial mask only - never full IV
                IVTextBox.Text = MaskValue(_config.IV);
            }
        }

        private void KeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_keyMasked && !KeyTextBox.Text.Contains('•'))
            {
                bool isValid = ConfigManager.IsValidPlaintextKey(KeyTextBox.Text);
                KeyTextBox.BorderBrush = isValid 
                    ? (SolidColorBrush)FindResource("AccentGreen") 
                    : (SolidColorBrush)FindResource("AccentRed");
            }
        }

        private void IVTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ivMasked && !IVTextBox.Text.Contains('•'))
            {
                bool isValid = ConfigManager.IsValidPlaintextIV(IVTextBox.Text);
                IVTextBox.BorderBrush = isValid 
                    ? (SolidColorBrush)FindResource("AccentGreen") 
                    : (SolidColorBrush)FindResource("AccentRed");
            }
        }

        private void SaveKeysButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keyMasked || _ivMasked)
            {
                MessageBox.Show("Please show the keys (click 👁️) before saving.", "Keys Hidden", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string key = KeyTextBox.Text.Trim();
                string iv = IVTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(iv))
                {
                    MessageBox.Show("Key and IV cannot be empty.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ConfigManager.IsValidPlaintextKey(key))
                {
                    int byteCount = System.Text.Encoding.UTF8.GetByteCount(key);
                    MessageBox.Show($"Key must be exactly 16, 24, or 32 characters for AES.\n\nYour key has {byteCount} characters.", 
                        "Invalid Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ConfigManager.IsValidPlaintextIV(iv))
                {
                    MessageBox.Show("IV must be exactly 16 characters (bytes).", 
                        "Invalid IV", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save both plaintext and keep existing Base64 keys
                ConfigManager.SaveKeys(key, iv, _config.KeyBase64, _config.IVBase64);
                _config = ConfigManager.LoadKeys();
                UpdateStatus("✓ Keys saved successfully");
                MessageBox.Show("Keys saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving keys: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetKeysButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will reset the encryption keys to the default values.\n\nAre you sure?",
                "Reset Keys",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ConfigManager.ResetToDefaultKeys();
                    _config = ConfigManager.LoadKeys();
                    
                    _keyMasked = true;
                    _ivMasked = true;
                    KeyShowButton.Content = "👁️";
                    IVShowButton.Content = "👁️";
                    KeyTextBox.Text = new string('•', 16);
                    IVTextBox.Text = new string('•', 16);
                    
                    UpdateStatus("✓ Keys reset to default");
                    MessageBox.Show("Keys have been reset to default values.", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting keys: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetDefaultKeysButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keyMasked || _ivMasked)
            {
                MessageBox.Show("Please show the keys (click 👁️) first to set them as default.", "Keys Hidden", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string key = KeyTextBox.Text.Trim();
            string iv = IVTextBox.Text.Trim();

            if (!ConfigManager.IsValidPlaintextKey(key))
            {
                MessageBox.Show("Key must be 16, 24, or 32 characters.", "Invalid Key", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ConfigManager.IsValidPlaintextIV(iv))
            {
                MessageBox.Show("IV must be 16 characters.", "Invalid IV", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "This will set the current keys as the new default.\n\nThe 'Reset' button will restore to these keys in the future.\n\nContinue?",
                "Set as Default",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ConfigManager.SetAsDefaultKeys(key, iv);
                    ConfigManager.SaveKeys(key, iv, _config.KeyBase64, _config.IVBase64);
                    _config = ConfigManager.LoadKeys();
                    UpdateStatus("✓ Keys set as new default");
                    MessageBox.Show("Current keys are now the default!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Encrypt Functions
        private void EncryptInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EncryptAutoDetectCheckBox.IsChecked == true)
            {
                bool looksEncrypted = AESCryptography.LooksLikeEncrypted(EncryptInputTextBox.Text);
                if (looksEncrypted)
                {
                    UpdateStatus("⚠ Detected encrypted text - try Decrypt panel");
                }
                else if (!string.IsNullOrWhiteSpace(EncryptInputTextBox.Text))
                {
                    UpdateStatus("Ready to encrypt (double encryption)");
                }
            }
            ValidateInput(EncryptInputTextBox, true);
        }

        private void EncryptInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EncryptButton_Click(sender, e);
            }
        }

        private void EncryptPasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                EncryptInputTextBox.Text = Clipboard.GetText().Trim();
                EncryptInputTextBox.Focus();
            }
        }

        private void EncryptFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            _encryptFavorite = !_encryptFavorite;
            EncryptFavoriteButton.Content = _encryptFavorite ? "🔖" : "🏷️";
            EncryptFavoriteButton.ToolTip = _encryptFavorite ? "Remove bookmark" : "Add to bookmarks";
            
            if (_currentEncryptEntry != null)
            {
                _currentEncryptEntry.IsFavorite = _encryptFavorite;
                HistoryManager.UpdateEntry(_currentEncryptEntry);
                RefreshHistory();
            }
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = EncryptInputTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    UpdateStatus("⚠ Error: Input is empty");
                    return;
                }

                // Use double encryption with plaintext keys
                string encrypted = AESCryptography.Encrypt(input, _config.Key, _config.IV);
                EncryptOutputTextBox.Text = encrypted;

                if (_settings.AutoCopy)
                {
                    Clipboard.SetText(encrypted);
                    UpdateStatus($"✓ Double encrypted and copied - {DateTime.Now:HH:mm:ss}");
                }
                else
                {
                    UpdateStatus($"✓ Double encrypted - {DateTime.Now:HH:mm:ss}");
                }

                _currentEncryptEntry = new HistoryEntry
                {
                    Operation = "encrypt",
                    Input = input,
                    Output = encrypted,
                    Note = EncryptNoteTextBox.Text,
                    IsFavorite = _encryptFavorite
                };
                HistoryManager.AddEntry(_currentEncryptEntry);
                RefreshHistory();
                RefreshRecentItems();

                EncryptInputTextBox.SelectAll();
                EncryptInputTextBox.Focus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"✗ Error: {ex.Message}");
                MessageBox.Show($"Encryption failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EncryptClearButton_Click(object sender, RoutedEventArgs e)
        {
            EncryptInputTextBox.Clear();
            EncryptOutputTextBox.Clear();
            EncryptNoteTextBox.Clear();
            _encryptFavorite = false;
            EncryptFavoriteButton.Content = "🏷️";
            EncryptFavoriteButton.ToolTip = "Add to bookmarks";
            _currentEncryptEntry = null;
            EncryptInputTextBox.Focus();
            UpdateStatus("✓ Ready");
        }

        private void EncryptOutputTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(EncryptOutputTextBox.Text))
            {
                Clipboard.SetText(EncryptOutputTextBox.Text);
                UpdateStatus("✓ Copied to clipboard");
            }
        }

        // Decrypt Functions
        private void DecryptInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DecryptAutoDetectCheckBox.IsChecked == true)
            {
                bool looksEncrypted = AESCryptography.LooksLikeEncrypted(DecryptInputTextBox.Text);
                if (looksEncrypted)
                {
                    UpdateStatus("Ready to decrypt (double decryption)");
                }
                else if (!string.IsNullOrWhiteSpace(DecryptInputTextBox.Text))
                {
                    UpdateStatus("⚠ Detected plain text - try Encrypt panel");
                }
            }
            ValidateInput(DecryptInputTextBox, false);
        }

        private void DecryptInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DecryptButton_Click(sender, e);
            }
        }

        private void DecryptPasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                DecryptInputTextBox.Text = Clipboard.GetText().Trim();
                DecryptInputTextBox.Focus();
            }
        }

        private void DecryptFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            _decryptFavorite = !_decryptFavorite;
            DecryptFavoriteButton.Content = _decryptFavorite ? "🔖" : "🏷️";
            DecryptFavoriteButton.ToolTip = _decryptFavorite ? "Remove bookmark" : "Add to bookmarks";
            
            if (_currentDecryptEntry != null)
            {
                _currentDecryptEntry.IsFavorite = _decryptFavorite;
                HistoryManager.UpdateEntry(_currentDecryptEntry);
                RefreshHistory();
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = DecryptInputTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    UpdateStatus("⚠ Error: Input is empty");
                    return;
                }

                // Use double decryption with plaintext keys
                string decrypted = AESCryptography.Decrypt(input, _config.Key, _config.IV);
                DecryptOutputTextBox.Text = decrypted;

                if (_settings.AutoCopy)
                {
                    Clipboard.SetText(decrypted);
                    UpdateStatus($"✓ Double decrypted and copied - {DateTime.Now:HH:mm:ss}");
                }
                else
                {
                    UpdateStatus($"✓ Double decrypted - {DateTime.Now:HH:mm:ss}");
                }

                _currentDecryptEntry = new HistoryEntry
                {
                    Operation = "decrypt",
                    Input = input,
                    Output = decrypted,
                    Note = DecryptNoteTextBox.Text,
                    IsFavorite = _decryptFavorite
                };
                HistoryManager.AddEntry(_currentDecryptEntry);
                RefreshHistory();
                RefreshRecentItems();

                DecryptInputTextBox.SelectAll();
                DecryptInputTextBox.Focus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"✗ Error: {ex.Message}");
                MessageBox.Show($"Decryption failed: {ex.Message}\n\nMake sure the input is valid encrypted text and the correct keys are configured.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptClearButton_Click(object sender, RoutedEventArgs e)
        {
            DecryptInputTextBox.Clear();
            DecryptOutputTextBox.Clear();
            DecryptNoteTextBox.Clear();
            _decryptFavorite = false;
            DecryptFavoriteButton.Content = "🏷️";
            DecryptFavoriteButton.ToolTip = "Add to bookmarks";
            _currentDecryptEntry = null;
            DecryptInputTextBox.Focus();
            UpdateStatus("✓ Ready");
        }

        private void DecryptOutputTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(DecryptOutputTextBox.Text))
            {
                Clipboard.SetText(DecryptOutputTextBox.Text);
                UpdateStatus("✓ Copied to clipboard");
            }
        }

        // Input Validation
        private void ValidateInput(TextBox textBox, bool isEncrypt)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.BorderBrush = (SolidColorBrush)FindResource("BorderColor");
                return;
            }

            if (isEncrypt)
            {
                textBox.BorderBrush = (SolidColorBrush)FindResource("AccentGreen");
            }
            else
            {
                bool isValid = AESCryptography.LooksLikeEncrypted(textBox.Text);
                textBox.BorderBrush = isValid 
                    ? (SolidColorBrush)FindResource("AccentGreen") 
                    : (SolidColorBrush)FindResource("AccentRed");
            }
        }

        // History Functions
        private void HistoryDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Note" && e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is HistoryEntry entry && e.EditingElement is TextBox textBox)
                {
                    // Update note with new value
                    string newNote = textBox.Text;
                    if (entry.Note != newNote)
                    {
                        entry.Note = newNote;
                        HistoryManager.UpdateEntry(entry);
                        RefreshRecentItems();
                        UpdateStatus("✓ Note saved");
                    }
                }
            }
        }

        private void ShowBookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            // Redundant with new tab, but keeping empty handler or removing if button is gone
            // Logic moved to Tabs
        }

        private void HistoryLabel_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Click History label to refresh history
            RefreshHistory();
            UpdateStatus("History refreshed");
        }

        private void ShowAllHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Keep for backward compatibility
            RefreshHistory();
            UpdateStatus("Showing all history");
        }

        private void HistoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click to copy output
            if (HistoryDataGrid.SelectedItem is HistoryEntry entry)
            {
                Clipboard.SetText(entry.Output);
                UpdateStatus("✓ Copied to clipboard");
            }
        }

        private void HistoryFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                entry.IsFavorite = !entry.IsFavorite;
                HistoryManager.UpdateEntry(entry);
                RefreshHistory();
                UpdateStatus(entry.IsFavorite ? "🔖 Added to bookmarks" : "🏷️ Removed from bookmarks");
            }
        }

        private void HistoryCopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                Clipboard.SetText(entry.Output);
                UpdateStatus("✓ Copied to clipboard");
            }
        }

        private void HistoryEditNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                var dialog = new NoteEditDialog(entry.Note);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    entry.Note = dialog.NoteText;
                    HistoryManager.UpdateEntry(entry);
                    RefreshHistory();
                    RefreshRecentItems();
                    UpdateStatus("✓ Note updated");
                }
            }
        }

        private void HistoryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                var result = MessageBox.Show(
                    $"Delete this entry?\n\nInput: {entry.Input.Substring(0, Math.Min(30, entry.Input.Length))}...",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    HistoryManager.DeleteEntry(entry.Id);
                    RefreshHistory();
                    RefreshRecentItems();
                    UpdateStatus("✓ Entry deleted");
                }
            }
        }

        private void RecentItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                Clipboard.SetText(entry.Output);
                UpdateStatus("✓ Recent item copied to clipboard");
            }
        }

        // Settings
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _settings = settingsWindow.Settings;
                ConfigManager.SaveSettings(_settings);
                EncryptAutoDetectCheckBox.IsChecked = _settings.AutoDetect;
                DecryptAutoDetectCheckBox.IsChecked = _settings.AutoDetect;
                RefreshRecentItems();
                UpdateStatus("✓ Settings saved");
            }
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
            
            // Color based on message type
            if (message.StartsWith("⚠") || message.StartsWith("✗"))
            {
                // Warning or error - red/orange
                StatusTextBlock.Foreground = (SolidColorBrush)FindResource("AccentRed");
            }
            else if (message.StartsWith("✓") || message.StartsWith("🔖"))
            {
                // Success - green
                StatusTextBlock.Foreground = (SolidColorBrush)FindResource("AccentGreen");
            }
            else
            {
                // Normal - default gray
                StatusTextBlock.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
                _settings.WindowLeft = Left;
                _settings.WindowTop = Top;
            }
            _settings.IsMaximized = WindowState == WindowState.Maximized;
            ConfigManager.SaveSettings(_settings);

            base.OnClosing(e);
        }

        private void HistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
