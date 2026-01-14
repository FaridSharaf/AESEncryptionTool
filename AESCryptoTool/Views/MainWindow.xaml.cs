using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AESCryptoTool.Models;
using AESCryptoTool.Services;

// Resolve WPF vs WinForms conflicts
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using DragEventArgs = System.Windows.DragEventArgs;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;

namespace AESCryptoTool.Views
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
        // Batch processing
        private string? _batchFilePath;
        private CancellationTokenSource? _batchCancellationSource;
        // private bool _showingFavoritesOnly = false; // Removed
        
        // Use ObservableCollection to prevent grid resets
        public System.Collections.ObjectModel.ObservableCollection<HistoryEntry> HistoryItems { get; set; } = new();
        public System.Collections.ObjectModel.ObservableCollection<HistoryEntry> BookmarksItems { get; set; } = new();
        
        // System Tray
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            
            // Register window command bindings for custom title bar buttons
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) => {
                if (this.WindowState == WindowState.Maximized)
                    SystemCommands.RestoreWindow(this);
                else
                    SystemCommands.MaximizeWindow(this);
            }));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (s, e) => SystemCommands.RestoreWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
            
            LoadSettings();
            LoadKeys();
            ApplyThemeOnLoad();
            
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
            SetupSystemTray();
            
            // Subscribe to window events for tray behavior
            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;
            
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

        #region Theme Management
        
        private void ApplyThemeOnLoad()
        {
            var savedTheme = ThemeManager.LoadSavedTheme();
            ApplyTheme(savedTheme);
        }
        
        public void ApplyTheme(string themeName)
        {
            ThemeManager.ApplyTheme(themeName);
        }
        
        #endregion

        // Settings
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings, this);
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
            var result = CustomMessageBox.Show(
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
            var result = CustomMessageBox.Show(
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
                CustomMessageBox.Show("Please show the keys (click 👁️) before saving.", "Keys Hidden", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string key = KeyTextBox.Text.Trim();
                string iv = IVTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(iv))
                {
                    CustomMessageBox.Show("Key and IV cannot be empty.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ConfigManager.IsValidPlaintextKey(key))
                {
                    int byteCount = System.Text.Encoding.UTF8.GetByteCount(key);
                    CustomMessageBox.Show($"Key must be exactly 16, 24, or 32 characters for AES.\n\nYour key has {byteCount} characters.", 
                        "Invalid Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ConfigManager.IsValidPlaintextIV(iv))
                {
                    CustomMessageBox.Show("IV must be exactly 16 characters (bytes).", 
                        "Invalid IV", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save both plaintext and keep existing Base64 keys
                ConfigManager.SaveKeys(key, iv, _config.KeyBase64, _config.IVBase64);
                _config = ConfigManager.LoadKeys();
                UpdateStatus("✓ Keys saved successfully");
                CustomMessageBox.Show("Keys saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error saving keys: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetKeysButton_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.Show(
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
                    CustomMessageBox.Show("Keys have been reset to default values.", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error resetting keys: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetDefaultKeysButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keyMasked || _ivMasked)
            {
                CustomMessageBox.Show("Please show the keys (click 👁️) first to set them as default.", "Keys Hidden", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string key = KeyTextBox.Text.Trim();
            string iv = IVTextBox.Text.Trim();

            if (!ConfigManager.IsValidPlaintextKey(key))
            {
                CustomMessageBox.Show("Key must be 16, 24, or 32 characters.", "Invalid Key", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ConfigManager.IsValidPlaintextIV(iv))
            {
                CustomMessageBox.Show("IV must be 16 characters.", "Invalid IV", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = CustomMessageBox.Show(
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
                    CustomMessageBox.Show("Current keys are now the default!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error: {ex.Message}", "Error", 
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
                CustomMessageBox.Show($"Encryption failed: {ex.Message}", "Error", 
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
                CustomMessageBox.Show($"Decryption failed: {ex.Message}\n\nMake sure the input is valid encrypted text and the correct keys are configured.", 
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
                var result = CustomMessageBox.Show(
                    $"Delete this entry from HISTORY?\n(It will remain in Bookmarks if bookmarked)\n\nInput: {entry.Input.Substring(0, Math.Min(30, entry.Input.Length))}...",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    HistoryManager.DeleteFromHistory(entry.Id);
                    RefreshHistory();
                    RefreshRecentItems();
                    UpdateStatus("✓ Entry deleted from History");
                }
            }
        }

        private void BookmarksDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                var result = CustomMessageBox.Show(
                    $"Remove this bookmark?\n(It will remain in History)\n\nInput: {entry.Input.Substring(0, Math.Min(30, entry.Input.Length))}...",
                    "Confirm Remove Bookmark",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    HistoryManager.DeleteFromBookmarks(entry.Id);
                    RefreshHistory();
                    // RefreshRecentItems(); // Bookmarks changes might affect recent items status
                    UpdateStatus("✓ Bookmark removed");
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

        public void RefreshData()
        {
            RefreshHistory();
            RefreshRecentItems();
        }

        #region Batch Processing

        private void BrowseBatchFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Supported Files|*.xlsx;*.csv|Excel Files|*.xlsx|CSV Files|*.csv",
                Title = "Select file to process"
            };

            if (dialog.ShowDialog() == true)
            {
                _batchFilePath = dialog.FileName;
                BatchFilePathTextBox.Text = System.IO.Path.GetFileName(_batchFilePath);
                BatchFilePathTextBox.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                BatchProcessButton.IsEnabled = true;

                // Load columns and auto-detect
                LoadBatchColumns();
                UpdateBatchOutputPath();
            }
        }

        private void LoadBatchColumns()
        {
            if (string.IsNullOrEmpty(_batchFilePath)) return;

            bool hasHeader = BatchHasHeaderCheckBox.IsChecked == true;
            var headers = BatchProcessor.GetFileHeaders(_batchFilePath, hasHeader);

            BatchColumnComboBox.Items.Clear();
            foreach (var header in headers)
            {
                BatchColumnComboBox.Items.Add(header);
            }

            // Disable and dim pattern when no header
            BatchPatternTextBox.IsEnabled = hasHeader;
            BatchPatternLabel.Foreground = hasHeader 
                ? (SolidColorBrush)FindResource("TextSecondary") 
                : (SolidColorBrush)FindResource("TextMuted");
            BatchPatternTextBox.Foreground = hasHeader
                ? (SolidColorBrush)FindResource("TextPrimary")
                : (SolidColorBrush)FindResource("TextMuted");

            if (!hasHeader && headers.Count > 0)
            {
                // No header - auto-select first column
                BatchColumnComboBox.SelectedIndex = 0;
            }
            else if (headers.Count > 0)
            {
                // Auto-select column matching "PhoneNumber"
                string pattern = BatchPatternTextBox.Text;
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    foreach (var header in headers)
                    {
                        if (regex.IsMatch(header))
                        {
                            BatchColumnComboBox.SelectedItem = header;
                            break;
                        }
                    }
                }
                catch { }
            }

            BatchColumnComboBox.IsEnabled = headers.Count > 0;
            
            // Update row count display
            UpdateBatchRowCount();
        }

        private void BatchColumn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_batchFilePath) || BatchColumnComboBox.SelectedItem == null) return;

            // Auto-detect if data is encrypted or plaintext
            AutoDetectBatchOperation();
            UpdateBatchOutputPath();
            LoadBatchPreview();
        }

        private void AutoDetectBatchOperation()
        {
            if (string.IsNullOrEmpty(_batchFilePath)) return;

            try
            {
                bool hasHeader = BatchHasHeaderCheckBox.IsChecked == true;
                string columnName;

                // Get column name - use selected item or default to first column
                if (BatchColumnComboBox.SelectedItem != null)
                {
                    columnName = BatchColumnComboBox.SelectedItem.ToString() ?? "Column 1";
                }
                else
                {
                    // Fallback: use first column
                    columnName = hasHeader ? "" : "Column 1";
                    if (string.IsNullOrEmpty(columnName)) return;
                }
                
                // Get first value from selected column
                string? firstValue = BatchProcessor.GetFirstValue(_batchFilePath, columnName, hasHeader);
                
                if (!string.IsNullOrEmpty(firstValue))
                {
                    // Check if it looks like Base64 (encrypted)
                    bool looksEncrypted = IsBase64String(firstValue) && firstValue.Length > 20;
                    
                    if (looksEncrypted)
                    {
                        BatchDecryptRadio.IsChecked = true;
                    }
                    else
                    {
                        BatchEncryptRadio.IsChecked = true;
                    }
                }
            }
            catch { }
        }

        private bool IsBase64String(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            try
            {
                // Check if it matches Base64 pattern
                var regex = new System.Text.RegularExpressions.Regex(@"^[A-Za-z0-9+/=]+$");
                return regex.IsMatch(value) && value.Length % 4 == 0;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateBatchOutputPath()
        {
            if (string.IsNullOrEmpty(_batchFilePath)) return;

            bool encrypt = BatchEncryptRadio.IsChecked == true;
            string suffix = encrypt ? "_encrypted" : "_decrypted";
            string dir = System.IO.Path.GetDirectoryName(_batchFilePath) ?? "";
            string name = System.IO.Path.GetFileNameWithoutExtension(_batchFilePath);
            string ext = System.IO.Path.GetExtension(_batchFilePath);

            _batchOutputPath = System.IO.Path.Combine(dir, name + suffix + ext);
            _updatingOutputPath = true;
            BatchOutputPathTextBox.Text = System.IO.Path.GetFileName(_batchOutputPath);
            _updatingOutputPath = false;
            BatchOutputPathTextBox.Foreground = (SolidColorBrush)FindResource("TextPrimary");
        }

        private string? _batchOutputPath;
        private bool _updatingOutputPath = false;

        private void BrowseOutputFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_batchFilePath)) return;

            var extension = System.IO.Path.GetExtension(_batchFilePath);
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = extension == ".xlsx" ? "Excel Files|*.xlsx" : "CSV Files|*.csv",
                FileName = System.IO.Path.GetFileName(_batchOutputPath ?? "output" + extension),
                InitialDirectory = System.IO.Path.GetDirectoryName(_batchFilePath),
                Title = "Choose output location"
            };

            if (saveDialog.ShowDialog() == true)
            {
                _batchOutputPath = saveDialog.FileName;
                BatchOutputPathTextBox.Text = System.IO.Path.GetFileName(_batchOutputPath);
            }
        }

        private void BatchHasHeader_Changed(object sender, RoutedEventArgs e)
        {
            LoadBatchColumns();
            // Auto-detect runs via column selection change, but also call directly for no-header case
            AutoDetectBatchOperation();
            UpdateBatchOutputPath();
        }

        private void BatchOutputPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Skip if this is a programmatic update (not user edit)
            if (_updatingOutputPath) return;
            
            // Update stored path when user edits manually - construct full path
            if (!string.IsNullOrEmpty(BatchOutputPathTextBox.Text) && BatchOutputPathTextBox.Text != "(auto-generated)")
            {
                string dir = System.IO.Path.GetDirectoryName(_batchFilePath) ?? "";
                string userFilename = BatchOutputPathTextBox.Text;
                
                // If user typed just a filename, combine with input directory
                if (!System.IO.Path.IsPathRooted(userFilename))
                {
                    _batchOutputPath = System.IO.Path.Combine(dir, userFilename);
                }
                else
                {
                    _batchOutputPath = userFilename;
                }
                BatchOutputPathTextBox.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            }
        }

        private void CopyBatchSummary_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(BatchSummaryText.Text);
            UpdateStatus("✓ Summary copied to clipboard");
        }

        private async void BatchProcess_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_batchFilePath))
            {
                UpdateStatus("⚠ Please select a file first");
                return;
            }

            // Use stored output path or prompt
            string outputPath = _batchOutputPath ?? "";
            if (string.IsNullOrEmpty(outputPath))
            {
                UpdateBatchOutputPath();
                outputPath = _batchOutputPath ?? "";
            }

            bool encrypt = BatchEncryptRadio.IsChecked == true;
            bool hasHeader = BatchHasHeaderCheckBox.IsChecked == true;
            string pattern = BatchColumnComboBox.SelectedItem?.ToString() ?? BatchPatternTextBox.Text;

            // Prepare UI
            BatchProcessButton.IsEnabled = false;
            BatchCancelButton.IsEnabled = true;
            BatchProgressBar.Value = 0;
            BatchSummaryBorder.Visibility = Visibility.Collapsed;
            BatchPreviewBorder.Visibility = Visibility.Collapsed; // Hide preview during processing
            _batchCancellationSource = new CancellationTokenSource();

            var processor = new BatchProcessor(_config.Key, _config.IV);
            long lastUpdate = 0;
            processor.ProgressChanged += (current, total, _) =>
            {
                long now = DateTime.Now.Ticks;
                if (now - lastUpdate > 500000) // 50ms (10,000 ticks per ms)
                {
                    lastUpdate = now;
                    Dispatcher.Invoke(() =>
                    {
                        double percent = (double)current / total * 100;
                        BatchProgressBar.Value = percent;
                        BatchProgressText.Text = $"Processing row {current} of {total}...";
                    });
                }
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = await processor.ProcessFileAsync(
                    _batchFilePath,
                    outputPath,
                    pattern,
                    hasHeader,
                    encrypt,
                    _batchCancellationSource.Token);

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                // Show summary
                BatchSummaryBorder.Visibility = Visibility.Visible;
                if (result.Success)
                {
                    BatchProgressBar.Value = 100;
                    BatchProgressText.Text = "✓ Complete!";
                    BatchSummaryText.Text = $"✅ Processing complete!\n\n" +
                        $"📊 Column: {result.TargetColumn}\n\n" +
                        $"✓ Processed: {result.ProcessedRows}\n" +
                        $"⏭ Skipped: {result.SkippedRows}\n" +
                        $"❌ Failed: {result.FailedRows}\n" +
                        $"⏱ Time: {result.Duration.TotalSeconds:F2}s\n\n" +
                        $"📁 Saved to:\n{result.OutputPath}";
                    UpdateStatus($"✓ Batch {(encrypt ? "encryption" : "decryption")} complete - {result.ProcessedRows} rows");
                }
                else
                {
                    BatchProgressText.Text = "❌ Error";
                    BatchSummaryText.Text = $"❌ Error: {result.ErrorMessage}";
                    UpdateStatus($"✗ Batch processing failed: {result.ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                BatchProgressText.Text = "Cancelled";
                BatchSummaryText.Text = "Operation was cancelled.";
                BatchSummaryBorder.Visibility = Visibility.Visible;
                UpdateStatus("Batch processing cancelled");
            }
            finally
            {
                BatchProcessButton.IsEnabled = true;
                BatchCancelButton.IsEnabled = false;
                _batchCancellationSource?.Dispose();
                _batchCancellationSource = null;
            }
        }

        private void BatchCancel_Click(object sender, RoutedEventArgs e)
        {
            _batchCancellationSource?.Cancel();
        }

        private void BatchReset_Click(object sender, RoutedEventArgs e)
        {
            // Reset fields
            _batchFilePath = "";
            _batchOutputPath = null;
            
            // Reset UI
            BatchFilePathTextBox.Text = "Select a file (.xlsx or .csv)...";
            BatchFilePathTextBox.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            
            BatchHasHeaderCheckBox.IsChecked = true;
            BatchColumnComboBox.Items.Clear();
            BatchColumnComboBox.IsEnabled = false;
            
            BatchPatternTextBox.Text = ".*PhoneNumber.*";
            BatchPatternTextBox.IsEnabled = true;
            
            BatchEncryptRadio.IsChecked = true;
            
            BatchOutputPathTextBox.Text = "(auto-generated)";
            BatchOutputPathTextBox.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            
            BatchProgressBar.Value = 0;
            BatchProgressText.Text = "Ready to process";
            
            BatchSummaryBorder.Visibility = Visibility.Collapsed;
            BatchSummaryText.Text = "";
            
            // Reset preview
            BatchPreviewBorder.Visibility = Visibility.Collapsed;
            BatchPreviewDataGrid.ItemsSource = null;
            BatchRowCountText.Text = "";
            
            // Reset buttons
            BatchProcessButton.IsEnabled = false;
            BatchCancelButton.IsEnabled = false;
            
            UpdateStatus("Ready");
        }

        private void BatchBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    var ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".csv")
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        #region Global Drag & Drop
        
        private bool IsValidBatchFile(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    var ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    return ext == ".xlsx" || ext == ".csv";
                }
            }
            return false;
        }

        private void MainGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (IsValidBatchFile(e))
            {
                DropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void MainGrid_DragLeave(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        private void MainGrid_DragOver(object sender, DragEventArgs e)
        {
            if (IsValidBatchFile(e))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void MainGrid_Drop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    var ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".csv")
                    {
                        // Switch to Batch tab
                        MainTabControl.SelectedIndex = 1; // Batch is the 2nd tab (index 1)
                        
                        // Load the file
                        _batchFilePath = files[0];
                        BatchFilePathTextBox.Text = System.IO.Path.GetFileName(_batchFilePath);
                        BatchFilePathTextBox.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                        BatchProcessButton.IsEnabled = true;

                        LoadBatchColumns();
                        UpdateBatchOutputPath();
                        UpdateStatus($"✓ Loaded: {System.IO.Path.GetFileName(_batchFilePath)}");
                        
                        e.Handled = true;
                    }
                }
            }
        }
        
        #endregion

        private void BatchBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    var ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".csv")
                    {
                        _batchFilePath = files[0];
                        BatchFilePathTextBox.Text = System.IO.Path.GetFileName(_batchFilePath);
                        BatchFilePathTextBox.Foreground = (SolidColorBrush)FindResource("TextPrimary");
                        BatchProcessButton.IsEnabled = true;

                        // Load columns and auto-detect
                        LoadBatchColumns();
                        UpdateBatchOutputPath();
                        UpdateStatus($"✓ Loaded: {System.IO.Path.GetFileName(_batchFilePath)}");
                    }
                    else
                    {
                        UpdateStatus("⚠ Unsupported file type. Use .xlsx or .csv");
                    }
                }
            }
        }

        private void LoadBatchPreview()
        {
            if (string.IsNullOrEmpty(_batchFilePath) || BatchColumnComboBox.SelectedItem == null)
            {
                BatchPreviewBorder.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                bool hasHeader = BatchHasHeaderCheckBox.IsChecked == true;
                string columnName = BatchColumnComboBox.SelectedItem.ToString() ?? "";
                
                // Get 3 preview rows
                var previewValues = BatchProcessor.GetPreviewRows(_batchFilePath, columnName, hasHeader, 3);
                int totalRows = BatchProcessor.GetRowCount(_batchFilePath, hasHeader);
                
                if (previewValues.Count > 0)
                {
                    var previewItems = previewValues.Select((v, i) => new { RowNumber = i + 1, Value = v }).ToList();
                    BatchPreviewDataGrid.ItemsSource = previewItems;
                    BatchPreviewHeader.Text = $"📋 Preview (first {previewValues.Count} of {totalRows:N0} rows)";
                    BatchPreviewBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    BatchPreviewBorder.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                BatchPreviewBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateBatchRowCount()
        {
            if (string.IsNullOrEmpty(_batchFilePath))
            {
                BatchRowCountText.Text = "";
                return;
            }

            try
            {
                bool hasHeader = BatchHasHeaderCheckBox.IsChecked == true;
                int rowCount = BatchProcessor.GetRowCount(_batchFilePath, hasHeader);
                BatchRowCountText.Text = $"📊 {rowCount:N0} rows";
            }
            catch
            {
                BatchRowCountText.Text = "";
            }
        }

        private void ShowBatchLog(BatchResult result)
        {
            // Display processing log in preview area
            var logItems = new List<object>();
            
            if (result.Success)
            {
                logItems.Add(new { RowNumber = "✓", Value = $"Processed: {result.ProcessedRows} rows" });
                if (result.SkippedRows > 0)
                    logItems.Add(new { RowNumber = "⏭", Value = $"Skipped: {result.SkippedRows} rows" });
                if (result.FailedRows > 0)
                    logItems.Add(new { RowNumber = "❌", Value = $"Failed: {result.FailedRows} rows" });
                logItems.Add(new { RowNumber = "⏱", Value = $"Time: {result.Duration.TotalSeconds:F2}s" });
            }
            else
            {
                logItems.Add(new { RowNumber = "❌", Value = $"Error: {result.ErrorMessage}" });
            }

            BatchPreviewDataGrid.ItemsSource = logItems;
            BatchPreviewHeader.Text = result.Success ? "📋 Processing Log" : "📋 Error Log";
            BatchPreviewBorder.Visibility = Visibility.Visible;
        }

        private void OpenBatchOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_batchOutputPath)) return;
            string? dir = System.IO.Path.GetDirectoryName(_batchOutputPath);
            if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }

        #endregion

        #region System Tray

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            
            // Load icon from application resources
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aes_crypto_tool.ico");
            if (System.IO.File.Exists(iconPath))
            {
                _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                // Fallback: use default icon
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            _notifyIcon.Text = "AES Crypto Tool";
            _notifyIcon.Visible = false;
            
            // Double-click to restore
            _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
            
            // Context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => RestoreFromTray());
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _settings.MinimizeToTray)
            {
                HideToTray();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_settings.CloseToTray)
            {
                e.Cancel = true;
                HideToTray();
            }
            else
            {
                // Clean up tray icon
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            }
        }

        private void HideToTray()
        {
            this.Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(1000, "AES Crypto Tool", "Running in system tray", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        private void ExitApplication()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }

        #endregion
    }
}
