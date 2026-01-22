using System.Globalization;
using System.Linq;
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
            return (value is bool b && b) ? "★" : "☆";
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
                return string.IsNullOrWhiteSpace(entry.Note) ? entry.Output : entry.Note;
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
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
        private bool _isBusy = false;
        // private bool _showingFavoritesOnly = false; // Removed
        
        // Use ObservableCollection to prevent grid resets
        public System.Collections.ObjectModel.ObservableCollection<HistoryEntry> HistoryItems { get; set; } = new();
        public System.Collections.ObjectModel.ObservableCollection<HistoryEntry> BookmarksItems { get; set; } = new();
        
        // System Tray
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        private Button? _minimizeButton;

        // Helper properties for Key Profile access
        private KeyProfile CurrentProfile => _config.Profiles.FirstOrDefault(p => p.Id == _config.SelectedProfileId) 
                                           ?? _config.Profiles.FirstOrDefault() 
                                           ?? new KeyProfile();
        private string CurrentKey => CurrentProfile.Key;
        private string CurrentIV => CurrentProfile.IV;

        public MainWindow()
        {
            InitializeComponent();
            Services.SnackbarService.Initialize(this.SnackbarPresenter);
            Services.DialogService.Initialize(this.RootContentDialog);
            
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
            
            // Set focus to Encrypt input by default, or load Dashboard if enabled
            this.Loaded += (s, e) => 
            {
                if (_settings.ShowDashboard)
                {
                    LoadDashboard();
                }
                else
                {
                    EncryptInputTextBox.Focus();
                }
            };
        }

        private void LoadSettings()
        {
            // Load Settings
            _settings = ConfigManager.LoadSettings();
            
            // Apply Window Settings
            if (_settings.WindowWidth > 0) Width = _settings.WindowWidth;
            if (_settings.WindowHeight > 0) Height = _settings.WindowHeight;
            if (_settings.WindowLeft >= 0) Left = _settings.WindowLeft;
            if (_settings.WindowTop >= 0) Top = _settings.WindowTop;
            if (_settings.IsMaximized) WindowState = WindowState.Maximized;
            
            // Apply Always On Top
            Topmost = _settings.AlwaysOnTop;
            
            // Check args for file opening
            if (System.Windows.Application.Current.Properties["Args"] is string[] args && args.Length > 0)
            {
                // Handle file opening logic here if needed
                // For now, just acknowledge the args
            }

            EncryptAutoDetectCheckBox.IsChecked = _settings.AutoDetect;
            DecryptAutoDetectCheckBox.IsChecked = _settings.AutoDetect;
            
            InitializeSettings();
        }

        private void LoadKeys()
        {
            _config = ConfigManager.LoadKeys();
            
            // Populate Profile Selector
            ProfileComboBox.ItemsSource = _config.Profiles;
            
            // Select the active profile
            var activeProfile = _config.Profiles.FirstOrDefault(p => p.Id == _config.SelectedProfileId);
            if (activeProfile != null)
            {
                ProfileComboBox.SelectedItem = activeProfile;
                UpdateUIForProfile(activeProfile);
            }
            else if (_config.Profiles.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }
        }

        private void UpdateUIForProfile(KeyProfile profile)
        {
             // Sync config props for compatibility
             _config.Key = profile.Key;
             _config.IV = profile.IV;
             _config.KeyBase64 = profile.KeyBase64;
             _config.IVBase64 = profile.IVBase64;
             
             // Update UI
             // Update UI
             if (_keyMasked)
                 KeyTextBox.Text = new string('•', _config.Key.Length > 0 ? _config.Key.Length : 16);
             else
                 KeyTextBox.Text = MaskValue(_config.Key); // Changed from raw _config.Key to MaskValue

             if (_ivMasked)
                 IVTextBox.Text = new string('•', _config.IV.Length > 0 ? _config.IV.Length : 16);
             else
                 IVTextBox.Text = MaskValue(_config.IV); // Consistent partial masking for IV too
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is KeyProfile profile)
            {
                _config.SelectedProfileId = profile.Id;
                UpdateUIForProfile(profile);
            }
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var existingNames = _config.Profiles.Select(p => p.Name).ToList();
            var dialog = new ProfileNameDialog(existingNames);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var (newKey, newIV) = ConfigManager.GenerateRandomKeys();
                var newProfile = new KeyProfile
                {
                    Name = dialog.ProfileName,
                    Key = newKey,
                    IV = newIV,
                    KeyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newKey)),
                    IVBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newIV)) // Not strictly necessary for plaintext flow but good to fill
                };
                
                _config.Profiles.Add(newProfile);
                // Refresh binding
                ProfileComboBox.ItemsSource = null;
                ProfileComboBox.ItemsSource = _config.Profiles;
                ProfileComboBox.SelectedItem = newProfile;
                
                ConfigManager.SaveConfiguration(_config);
                UpdateStatus($"✓ Profile '{newProfile.Name}' created");
            }
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is KeyProfile profile)
            {
                if (_config.Profiles.Count <= 1)
                {
                    await Services.DialogService.WarningAsync("Error", "Cannot delete the last profile.");
                    return;
                }
                
                var result = await Services.DialogService.ConfirmAsync("Confirm Delete", $"Are you sure you want to delete profile '{profile.Name}'?");
                if (result)
                {
                    _config.Profiles.Remove(profile);
                     ProfileComboBox.ItemsSource = null;
                     ProfileComboBox.ItemsSource = _config.Profiles;
                     ProfileComboBox.SelectedIndex = 0;
                     
                     ConfigManager.SaveConfiguration(_config);
                     UpdateStatus("✓ Profile deleted");
                }
            }
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
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _minimizeButton = GetTemplateChild("MinimizeButton") as Button;
            UpdateMinimizeTooltip();
            
            // PinButton is handled directly via XAML field 'PinButton'
            UpdatePinButtonState();
        }

        private async void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            await Services.DialogService.AlertAsync(
                "About AES Crypto Tool",
                "AES Crypto Tool v2.2.1\n\n" +
                "Advanced Encryption Standard (AES-256) utility for secure text and file processing.\n\n" +
                "Features:\n" +
                "• Encrypt/Decrypt text with custom or random keys\n" +
                "• Manage multiple Key Profiles securely\n" +
                "• Batch process Excel/CSV files (Encrypt/Decrypt columns)\n" +
                "• History & Bookmarks for quick access\n" +
                "• Secure Data Import/Export\n\n" +
                "Created by Farid Ahmed");
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
            Topmost = _settings.AlwaysOnTop;
            ConfigManager.SaveSettings(_settings); // Auto-save this preference
            UpdatePinButtonState();
        }

        private void UpdatePinButtonState()
        {
            if (PinButton != null)
            {
                // Update Icon to Filled/Outline based on state
                if (PinButton.Icon is Wpf.Ui.Controls.SymbolIcon symbolIcon)
                {
                    symbolIcon.Filled = _settings.AlwaysOnTop;
                }
                
                // Update Tooltip logic
                PinButton.ToolTip = _settings.AlwaysOnTop ? "Unpin (Always on Top)" : "Pin (Always on Top)";
                
                // Optional: Change Foreground to indicate active state
                if (_settings.AlwaysOnTop)
                    PinButton.Foreground = (System.Windows.Media.Brush)FindResource("SystemAccentColorPrimaryBrush"); // Or specific accent
                else
                    PinButton.Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorPrimaryBrush");
            }
        }

        private void UpdateMinimizeTooltip()
        {
            if (_minimizeButton != null)
            {
                _minimizeButton.ToolTip = _settings.MinimizeToTray ? "Minimize to Tray" : "Minimize";
            }
        }
        
        #endregion



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
                HistoryLabel.Text = $"History ({HistoryItems.Count})";
            }
            else
            {
                HistoryLabel.Text = "History";
            }

            // Ensure button states are correct (e.g. reset to "Clear" after reload)
            UpdateHistoryDeleteButtonState();
            UpdateBookmarksDeleteButtonState();
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

        private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = HistoryItems.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count > 0)
            {
                // Batch Delete Selected
                int count = selectedItems.Count;
                var result = await Services.DialogService.ConfirmAsync(
                    "Confirm Deletion",
                    $"Are you sure you want to delete {count} selected item(s)?");

                if (result)
                {
                    var selectedIds = selectedItems.Select(x => x.Id).ToList();
                    HistoryManager.DeleteHistoryEntries(selectedIds);
                    LoadHistory();
                    UpdateHistoryDeleteButtonState();
                    UpdateStatus("✓ Selected history entries deleted");
                }
            }
            else
            {
                // Clear All (Existing Logic)
                if (HistoryItems.Count == 0) return;

                var result = await Services.DialogService.ConfirmAsync(
                    "Confirm Clear",
                    "Are you sure you want to clear all encryption history?\n(Bookmarks will be preserved)");

                if (result)
                {
                    HistoryManager.ClearHistory();
                    LoadHistory();
                    UpdateHistoryDeleteButtonState();
                    UpdateStatus("✓ History cleared");
                }
            }
        }

        private void HistorySelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in HistoryItems) item.IsSelected = true;
            UpdateHistoryDeleteButtonState();
        }

        private void HistorySelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in HistoryItems) item.IsSelected = false;
            UpdateHistoryDeleteButtonState();
        }

        private void HistoryItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateHistoryDeleteButtonState();
        }

        private void UpdateHistoryDeleteButtonState()
        {
            int count = HistoryItems.Count(x => x.IsSelected);
            if (count > 0)
            {
                 ClearHistoryButton.Content = $"Delete Selected ({count})";
                 ClearHistoryButton.ToolTip = "Delete selected items";
            }
            else
            {
                 ClearHistoryButton.Content = "Clear History";
                 ClearHistoryButton.ToolTip = "Delete all history entries (reserved)";
            }
        }

        private async void ClearBookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = BookmarksItems.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count > 0)
            {
                // Delete Selected
                int count = selectedItems.Count;
                var result = await Services.DialogService.ConfirmAsync(
                    "Confirm Deletion",
                    $"Are you sure you want to delete {count} selected bookmark(s)?");

                if (result)
                {
                    var selectedIds = selectedItems.Select(x => x.Id).ToList();
                    HistoryManager.DeleteBookmarkEntries(selectedIds);
                    LoadHistory();
                    UpdateStatus("✓ Selected bookmarks deleted");
                    LoadHistory();
                    UpdateBookmarksDeleteButtonState();
                }
            }
            else
            {
                // Clear All
                if (BookmarksItems.Count == 0) return;

                var result = await Services.DialogService.ConfirmAsync(
                    "Confirm Clear",
                    "Are you sure you want to delete ALL bookmarks?");

                if (result)
                {
                    var allIds = BookmarksItems.Select(x => x.Id).ToList();
                    HistoryManager.DeleteBookmarkEntries(allIds);
                    LoadHistory();
                    UpdateBookmarksDeleteButtonState();
                    UpdateStatus("✓ Bookmarks cleared");
                }
            }
        }

        private void BookmarksSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in BookmarksItems) item.IsSelected = true;
            UpdateBookmarksDeleteButtonState();
        }

        private void BookmarksSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in BookmarksItems) item.IsSelected = false;
            UpdateBookmarksDeleteButtonState();
        }

        private void BookmarksItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBookmarksDeleteButtonState();
        }

        private void UpdateBookmarksDeleteButtonState()
        {
            int count = BookmarksItems.Count(x => x.IsSelected);
            if (count > 0)
            {
                 ClearBookmarksButton.Content = $"Delete Selected ({count})";
                 ClearBookmarksButton.ToolTip = "Delete selected items";
            }
            else
            {
                 ClearBookmarksButton.Content = "Clear Bookmarks";
                 ClearBookmarksButton.ToolTip = "Remove all bookmarks";
            }
        }

        private void RefreshRecentItems()
        {
            // Limit recent items based on settings (configurable up to 20)
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
            if (KeyShowButton.Icon is Wpf.Ui.Controls.SymbolIcon icon)
            {
                icon.Symbol = _keyMasked ? Wpf.Ui.Controls.SymbolRegular.Eye24 : Wpf.Ui.Controls.SymbolRegular.EyeOff24;
            }
            
            if (_keyMasked)
            {
                KeyTextBox.Text = new string('•', _config.Key.Length > 0 ? _config.Key.Length : 16);
            }
            else
            {
                // Show partial mask only - never full key
                KeyTextBox.Text = MaskValue(_config.Key);
            }
        }

        private void IVShowButton_Click(object sender, RoutedEventArgs e)
        {
            _ivMasked = !_ivMasked;
            if (IVShowButton.Icon is Wpf.Ui.Controls.SymbolIcon icon)
            {
                icon.Symbol = _ivMasked ? Wpf.Ui.Controls.SymbolRegular.Eye24 : Wpf.Ui.Controls.SymbolRegular.EyeOff24;
            }

            if (_ivMasked)
            {
                IVTextBox.Text = new string('•', _config.IV.Length > 0 ? _config.IV.Length : 16);
            }
            else
            {
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

        private async void SaveKeysButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keyMasked || _ivMasked)
            {
                await Services.DialogService.AlertAsync("Keys Hidden", "Please show the keys (click 👁️) before saving.");
                return;
            }

            try
            {
                string key = KeyTextBox.Text.Trim();
                string iv = IVTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(iv))
                {
                    await Services.DialogService.WarningAsync("Validation Error", "Key and IV cannot be empty.");
                    return;
                }

                if (!ConfigManager.IsValidPlaintextKey(key))
                {
                    int byteCount = System.Text.Encoding.UTF8.GetByteCount(key);
                    await Services.DialogService.WarningAsync("Invalid Key", $"Key must be exactly 16, 24, or 32 characters for AES.\n\nYour key has {byteCount} characters.");
                    return;
                }

                if (!ConfigManager.IsValidPlaintextIV(iv))
                {
                    await Services.DialogService.WarningAsync("Invalid IV", "IV must be exactly 16 characters (bytes).");
                    return;
                }

                // Update current profile
                if (ProfileComboBox.SelectedItem is KeyProfile profile)
                {
                    profile.Key = key;
                    profile.IV = iv;
                }

                // Update legacy props just in case
                _config.Key = key;
                _config.IV = iv;
                
                ConfigManager.SaveConfiguration(_config);
                
                // _config = ConfigManager.LoadKeys(); // Don't reload, it resets UI state
                UpdateStatus("✓ Keys saved successfully");
                await Services.DialogService.AlertAsync("Success", "Keys saved successfully!");
            }
            catch (Exception ex)
            {
                await Services.DialogService.ErrorAsync("Error", $"Error saving keys: {ex.Message}");
            }
        }

        private async void ResetKeysButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await Services.DialogService.ConfirmAsync(
                "Reset Keys",
                "This will reset the encryption keys to the default values.\n\nAre you sure?");

            if (result)
            {
                try
                {
                    ConfigManager.ResetToDefaultKeys();
                    _config = ConfigManager.LoadKeys();
                    
                    _keyMasked = true;
                    _ivMasked = true;
                    
                    if (KeyShowButton.Icon is Wpf.Ui.Controls.SymbolIcon keyIcon) keyIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Eye24;
                    if (IVShowButton.Icon is Wpf.Ui.Controls.SymbolIcon ivIcon) ivIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Eye24;

                    KeyTextBox.Text = new string('•', 16);
                    IVTextBox.Text = new string('•', 16);
                    
                    UpdateStatus("✓ Keys reset to default");
                    await Services.DialogService.AlertAsync("Success", "Keys have been reset to default values.");
                }
                catch (Exception ex)
                {
                    await Services.DialogService.ErrorAsync("Error", $"Error resetting keys: {ex.Message}");
                }
            }
        }

        private async void SetDefaultKeysButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keyMasked || _ivMasked)
            {
                await Services.DialogService.AlertAsync("Keys Hidden", "Please show the keys (click 👁️) first to set them as default.");
                return;
            }

            string key = KeyTextBox.Text.Trim();
            string iv = IVTextBox.Text.Trim();

            if (!ConfigManager.IsValidPlaintextKey(key))
            {
                await Services.DialogService.WarningAsync("Invalid Key", "Key must be 16, 24, or 32 characters.");
                return;
            }

            if (!ConfigManager.IsValidPlaintextIV(iv))
            {
                await Services.DialogService.WarningAsync("Invalid IV", "IV must be 16 characters.");
                return;
            }

            var result = await Services.DialogService.ConfirmAsync(
                "Set as Default",
                "This will set the current keys as the new default.\n\nThe 'Reset' button will restore to these keys in the future.\n\nContinue?");

            if (result)
            {
                try
                {
                    ConfigManager.SetAsDefaultKeys(key, iv);
                    ConfigManager.SetAsDefaultKeys(key, iv);
                    
                    // Update current profile
                    _config.Key = key;
                    _config.IV = iv;
                    if (ProfileComboBox.SelectedItem is KeyProfile profile)
                    {
                         profile.Key = key;
                         profile.IV = iv;
                    }
                    
                    ConfigManager.SaveConfiguration(_config);
                    _config = ConfigManager.LoadKeys();
                    UpdateStatus("✓ Keys set as new default");
                    await Services.DialogService.AlertAsync("Success", "Current keys are now the default!");
                }
                catch (Exception ex)
                {
                    await Services.DialogService.ErrorAsync("Error", $"Error: {ex.Message}");
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
            if (EncryptFavoriteButton.Icon is Wpf.Ui.Controls.SymbolIcon icon)
            {
                icon.Filled = _encryptFavorite;
            }
            EncryptFavoriteButton.ToolTip = _encryptFavorite ? "Remove bookmark" : "Add to bookmarks";
            
            if (_currentEncryptEntry != null)
            {
                _currentEncryptEntry.IsFavorite = _encryptFavorite;
                HistoryManager.UpdateEntry(_currentEncryptEntry);
                RefreshHistory();
            }
        }

        private async void EncryptButton_Click(object sender, RoutedEventArgs e)
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
                string encrypted = AESCryptography.Encrypt(input, CurrentKey, CurrentIV);
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
                await DialogService.ErrorAsync("Error", $"Encryption failed: {ex.Message}");
            }
        }

        private void EncryptClearButton_Click(object sender, RoutedEventArgs e)
        {
            EncryptInputTextBox.Clear();
            EncryptOutputTextBox.Clear();
            EncryptNoteTextBox.Clear();
            _encryptFavorite = false;
            if (EncryptFavoriteButton.Icon is Wpf.Ui.Controls.SymbolIcon icon)
            {
                icon.Filled = false;
            }
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
                e.Handled = true;
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
            if (DecryptFavoriteButton.Icon is Wpf.Ui.Controls.SymbolIcon icon)
            {
                icon.Filled = _decryptFavorite;
            }
            DecryptFavoriteButton.ToolTip = _decryptFavorite ? "Remove bookmark" : "Add to bookmarks";
            
            if (_currentDecryptEntry != null)
            {
                _currentDecryptEntry.IsFavorite = _decryptFavorite;
                HistoryManager.UpdateEntry(_currentDecryptEntry);
                RefreshHistory();
            }
        }

        private async void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;
            _isBusy = true;
            try
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
                string decrypted = AESCryptography.Decrypt(input, CurrentKey, CurrentIV);
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
                await DialogService.ErrorAsync("Error", $"Decryption failed: {ex.Message}\n\nMake sure the input is valid encrypted text and the correct keys are configured.");
            }
            }
            finally
            {
                _isBusy = false;
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
            // Double-click to copy output (works for both History and Bookmarks)
            if (sender is DataGrid grid && grid.SelectedItem is HistoryEntry entry)
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

        private async void HistoryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                var result = await DialogService.ConfirmAsync(
                    "Confirm Delete",
                    $"Delete this entry from HISTORY?\n(It will remain in Bookmarks if bookmarked)\n\nInput: {entry.Input.Substring(0, Math.Min(30, entry.Input.Length))}...");

                if (result)
                {
                    HistoryManager.DeleteFromHistory(entry.Id);
                    RefreshHistory();
                    RefreshRecentItems();
                    UpdateStatus("✓ Entry deleted from History");
                }
            }
        }

        private async void BookmarksDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HistoryEntry entry)
            {
                var result = await DialogService.ConfirmAsync(
                    "Confirm Remove Bookmark",
                    $"Remove this bookmark?\n(It will remain in History)\n\nInput: {entry.Input.Substring(0, Math.Min(30, entry.Input.Length))}...");

                if (result)
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
            Services.SnackbarService.Information("Summary copied to clipboard");
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

            var processor = new BatchProcessor(CurrentKey, CurrentIV);
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
                    BatchProgressText.Text = "Complete!";
                    BatchSummaryText.Text = $"PROCESSING COMPLETE\n\n" +
                        $"Column: {result.TargetColumn}\n\n" +
                        $"✓ Processed: {result.ProcessedRows}\n" +
                        $"⏭ Skipped: {result.SkippedRows}\n" +
                        $"❌ Failed: {result.FailedRows}\n" +
                        $"⏱ Time: {result.Duration.TotalSeconds:F2}s\n\n" +
                        $"Saved to:\n{result.OutputPath}";
                    UpdateStatus($"Batch {(encrypt ? "encryption" : "decryption")} complete - {result.ProcessedRows} rows");
                }
                else
                {
                    BatchProgressText.Text = "Error";
                    BatchSummaryText.Text = $"Error: {result.ErrorMessage}";
                    UpdateStatus($"Batch processing failed: {result.ErrorMessage}");
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
                        NavBatch.IsChecked = true;
                        
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
                
                // Get 2 preview rows
                var previewValues = BatchProcessor.GetPreviewRows(_batchFilePath, columnName, hasHeader, 2);
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
                BatchRowCountText.Text = $"{rowCount:N0} rows";
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
            BatchPreviewHeader.Text = result.Success ? "Processing Log" : "Error Log";
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
            
            // Load icon from application
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
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

        private bool _isExiting = false;

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // If explicit exit or not configured to minimize to tray, just close
            if (_isExiting || !_settings.CloseToTray)
            {
                 // Clean up tray icon
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
                return;
            }

            // Otherwise, minimize to tray
            e.Cancel = true;
            HideToTray();
        }

        private void HideToTray()
        {
            if (_notifyIcon == null) return;
            
            // Try to set icon if missing
            if (_notifyIcon.Icon == null)
            {
                try 
                { 
                     // Try extraction first, fallback to system icon
                     var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                     if (!string.IsNullOrEmpty(exePath))
                        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                     else
                        _notifyIcon.Icon = System.Drawing.SystemIcons.Application; 
                } 
                catch 
                { 
                    // Last resort
                    try { _notifyIcon.Icon = System.Drawing.SystemIcons.Application; } catch { }
                }
            }
            
            // If we still don't have an icon, we can't safely use the tray
            if (_notifyIcon.Icon == null) 
            {
                // Can't minimize to tray without an icon, so just minimize normally
                this.WindowState = WindowState.Minimized;
                return;
            }
            
            this.Hide();

            _notifyIcon.Visible = true;
            
            // Show notification only once
            if (!_settings.HasShownTrayNotification)
            {
                _notifyIcon.ShowBalloonTip(1000, "AES Crypto Tool", "Running in system tray", System.Windows.Forms.ToolTipIcon.Info);
                _settings.HasShownTrayNotification = true;
                ConfigManager.SaveSettings(_settings);
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
            _isExiting = true;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            System.Windows.Application.Current.Shutdown();
        }


        private void NavTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string tag)
            {
                if (OperationsSection == null) return;
                
                DashboardSection.Visibility = Visibility.Collapsed;
                OperationsSection.Visibility = Visibility.Collapsed;
                BatchSection.Visibility = Visibility.Collapsed;
                HistorySection.Visibility = Visibility.Collapsed;
                BookmarksSection.Visibility = Visibility.Collapsed;
                SettingsSection.Visibility = Visibility.Collapsed;

                if (StatusBarBorder != null)
                {
                    StatusBarBorder.Visibility = tag == "Dashboard" ? Visibility.Collapsed : Visibility.Visible;
                }

                switch (tag)
                {
                    case "Dashboard":  
                        DashboardSection.Visibility = Visibility.Visible; 
                        LoadDashboard();
                        break;
                    case "Operations": 
                        OperationsSection.Visibility = Visibility.Visible; 
                        EncryptInputTextBox.Focus();
                        break;
                    case "Batch": BatchSection.Visibility = Visibility.Visible; break;
                    case "History": HistorySection.Visibility = Visibility.Visible; break;
                    case "Bookmarks": BookmarksSection.Visibility = Visibility.Visible; break;
                    case "Settings": SettingsSection.Visibility = Visibility.Visible; break;
                }
            }
        }



        private void LoadDashboard()
        {
            if (DashboardRecentItemsControl == null) return;
            var recent = HistoryManager.GetRecentItems(3);
            DashboardRecentItemsControl.ItemsSource = recent;
        }

        private void DashboardEncrypt_Click(object sender, RoutedEventArgs e)
        {
            NavOperations.IsChecked = true;
            // Optional: Focus Encrypt Box?
        }

        private void DashboardDecrypt_Click(object sender, RoutedEventArgs e)
        {
            NavOperations.IsChecked = true;
            DecryptInputTextBox.Focus();
        }

        private void DashboardBatch_Click(object sender, RoutedEventArgs e)
        {
            NavBatch.IsChecked = true;
        }

        private void DashboardCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn && btn.Tag is string output)
            {
                 Clipboard.SetText(output);
                 Services.SnackbarService.Information("Copied to clipboard");
            }
        }

        #endregion
        #region Settings Tab Logic

        private bool _isSettingsInitializing = true;

        private void InitializeSettings()
        {
            _isSettingsInitializing = true;

            // Behavior
            if (AutoCopyCheckBox != null) AutoCopyCheckBox.IsChecked = _settings.AutoCopy;
            if (AutoDetectCheckBox != null) AutoDetectCheckBox.IsChecked = _settings.AutoDetect;

            // Appearance - Theme
            if (SettingsThemeComboBox != null)
            {
                SettingsThemeComboBox.Items.Clear();
                
                // Light Themes
                SettingsThemeComboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = "── Light Themes ──", 
                    IsEnabled = false, 
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8")) 
                });

                foreach (var theme in ThemeManager.GetLightThemes())
                {
                    var item = new ComboBoxItem { Content = theme, Tag = theme };
                    SettingsThemeComboBox.Items.Add(item);
                    if (theme == _settings.Theme) SettingsThemeComboBox.SelectedItem = item;
                }

                // Dark Themes
                SettingsThemeComboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = "── Dark Themes ──", 
                    IsEnabled = false, 
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8")) 
                });

                foreach (var theme in ThemeManager.GetDarkThemes())
                {
                    var item = new ComboBoxItem { Content = theme, Tag = theme };
                    SettingsThemeComboBox.Items.Add(item);
                    if (theme == _settings.Theme) SettingsThemeComboBox.SelectedItem = item;
                }
                
                if (SettingsThemeComboBox.SelectedItem == null) SettingsThemeComboBox.SelectedIndex = 1;
            }

            // Appearance - Font
            if (SettingsFontComboBox != null)
            {
                foreach (ComboBoxItem item in SettingsFontComboBox.Items)
                {
                    if (item.Tag?.ToString() == _settings.FontFamily)
                    {
                        SettingsFontComboBox.SelectedItem = item;
                        break;
                    }
                }
                if (SettingsFontComboBox.SelectedItem == null) SettingsFontComboBox.SelectedIndex = 0;
            }

            if (SettingsRecentCountTextBox != null) SettingsRecentCountTextBox.Text = _settings.RecentItemsCount.ToString();

            // Storage
            if (SettingsMaxHistoryTextBox != null) SettingsMaxHistoryTextBox.Text = _settings.MaxHistoryItems.ToString();
            if (SettingsMaxBookmarksTextBox != null) SettingsMaxBookmarksTextBox.Text = _settings.MaxBookmarkItems.ToString();

            // System
            if (MinimizeToTrayCheckBox != null) MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTray;
            if (CloseToTrayCheckBox != null) CloseToTrayCheckBox.IsChecked = _settings.CloseToTray;
            if (MinimizeToTrayCheckBox != null) MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTray;
            if (CloseToTrayCheckBox != null) CloseToTrayCheckBox.IsChecked = _settings.CloseToTray;
            if (AlwaysOnTopCheckBox != null) AlwaysOnTopCheckBox.IsChecked = _settings.AlwaysOnTop;
            
            // Dashboard
            if (ShowDashboardCheckBox != null) ShowDashboardCheckBox.IsChecked = _settings.ShowDashboard;
            UpdateDashboardVisibility();

            _isSettingsInitializing = false;
        }

        private void UpdateDashboardVisibility()
        {
            if (NavDashboard == null) return;

            if (_settings.ShowDashboard)
            {
                NavDashboard.Visibility = Visibility.Visible;
            }
            else
            {
                NavDashboard.Visibility = Visibility.Collapsed;
                // If we are currently on Dashboard but it's now hidden, switch to Operations
                if (NavDashboard.IsChecked == true)
                {
                    NavOperations.IsChecked = true;
                }
            }
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isSettingsInitializing) return;
            SaveSettings();
            
            // Sync specific settings if needed
            if (sender == AutoDetectCheckBox)
            {
                EncryptAutoDetectCheckBox.IsChecked = AutoDetectCheckBox.IsChecked;
                DecryptAutoDetectCheckBox.IsChecked = AutoDetectCheckBox.IsChecked;
            }
            if (sender == ShowDashboardCheckBox)
            {
                _settings.ShowDashboard = ShowDashboardCheckBox.IsChecked == true;
                UpdateDashboardVisibility();
            }
        }

        private void SettingsTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSettingsInitializing) return;
            if (SettingsThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string themeName)
            {
                _settings.Theme = themeName;
                ApplyTheme(themeName);
                SaveSettings();
            }
        }

        private void SettingsFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSettingsInitializing) return;
            if (SettingsFontComboBox.SelectedItem is ComboBoxItem item && item.Tag is string fontName)
            {
                _settings.FontFamily = fontName;
                Services.ThemeManager.ApplyFont(fontName);
                SaveSettings();
            }
        }

        private void AlwaysOnTop_Changed(object sender, RoutedEventArgs e)
        {
             if (_isSettingsInitializing) return;
             _settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
             Topmost = _settings.AlwaysOnTop;
             
             // Sync title bar pin button if it exists and has visual state
             // (PinButton click logic handles its own state, but we should keep them in sync if possible)
             if (PinButton.Icon is Wpf.Ui.Controls.SymbolIcon icon)
             {
                 icon.Filled = _settings.AlwaysOnTop;
             }
             
             SaveSettings();
        }

        // TextChanged Handlers
        private void SettingsRecentCount_TextChanged(object sender, TextChangedEventArgs e)
        {
             if (_isSettingsInitializing) return;
             if (int.TryParse(SettingsRecentCountTextBox.Text, out int val))
             {
                 if (val > 20) { val = 20; SettingsRecentCountTextBox.Text = "20"; SettingsRecentCountTextBox.CaretIndex = 2; }
                 if (val > 0) 
                 {
                     _settings.RecentItemsCount = val;
                     SaveSettings();
                 }
             }
        }

        private void SettingsMaxHistory_TextChanged(object sender, TextChangedEventArgs e)
        {
             if (_isSettingsInitializing) return;
             
             if (int.TryParse(SettingsMaxHistoryTextBox.Text, out int val))
             {
                 if (val > 1000) 
                 { 
                     val = 1000; 
                     SettingsMaxHistoryTextBox.Text = "1000"; 
                     SettingsMaxHistoryTextBox.CaretIndex = 4; 
                 }
                 
                 if (val > 0)
                 {
                     _settings.MaxHistoryItems = val;
                     SaveSettings(); 
                 }
             }
        }

        private void SettingsMaxBookmarks_TextChanged(object sender, TextChangedEventArgs e)
        {
             if (_isSettingsInitializing) return;
             
             if (int.TryParse(SettingsMaxBookmarksTextBox.Text, out int val))
             {
                 if (val > 1000) 
                 { 
                     val = 1000; 
                     SettingsMaxBookmarksTextBox.Text = "1000"; 
                     SettingsMaxBookmarksTextBox.CaretIndex = 4; 
                 }

                 if (val > 0)
                 {
                     _settings.MaxBookmarkItems = val;
                     SaveSettings();
                 }
             }
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                bool result = await Services.ImportExportService.ExportDataAsync(true, true);
                if (result) UpdateStatus("✓ Data exported successfully");
            }
            catch (Exception ex)
            {
                UpdateStatus($"⚠ Export failed: {ex.Message}");
            }
        }

        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
             try 
             {
                 int count = await Services.ImportExportService.ImportDataAsync();
                 if (count > 0)
                 {
                     UpdateStatus($"✓ Imported {count} items");
                     RefreshHistory();
                     if (this.BookmarksItems != null)
                     {
                        // Refresh bookmarks manually since ObservableCollection might need update
                        BookmarksItems.Clear();
                        foreach(var b in HistoryManager.GetFavorites()) BookmarksItems.Add(b);
                     }
                 }
                 else if (count == 0)
                 {
                     UpdateStatus("Import cancelled or empty");
                 }
                 else 
                 {
                     UpdateStatus("⚠ No valid data found for import");
                 }
             }
             catch (Exception ex)
             {
                 UpdateStatus($"⚠ Import failed: {ex.Message}");
             }
        }

        private void SaveSettings()
        {
            _settings.AutoCopy = AutoCopyCheckBox.IsChecked == true;
            _settings.AutoDetect = AutoDetectCheckBox.IsChecked == true;
            _settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
            _settings.CloseToTray = CloseToTrayCheckBox.IsChecked == true;
            
             ConfigManager.SaveSettings(_settings);
             
             if (SettingsStatusText != null)
             {
                 SettingsStatusText.Text = "Saved ✓";
                 // Could use a timer to clear it, but simple is fine for now
             }
        }

        #endregion
    }
}
