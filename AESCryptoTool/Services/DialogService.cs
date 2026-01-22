using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace AESCryptoTool.Services
{
    /// <summary>
    /// Service for displaying ContentDialog modals throughout the application.
    /// </summary>
    public static class DialogService
    {
        private static ContentPresenter? _presenter;

        public static void Initialize(ContentPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>
        /// Shows a dialog with customizable buttons. Returns the result.
        /// </summary>
        public static async Task<ContentDialogResult> ShowAsync(
            string title, 
            string message, 
            string primaryButtonText = "OK", 
            string? closeButtonText = null,
            string? secondaryButtonText = null)
        {
            if (_presenter == null) 
                throw new InvalidOperationException("DialogService not initialized. Call Initialize() first.");

            var dialog = new ContentDialog(_presenter)
            {
                Title = title,
                IsFooterVisible = false,
                DefaultButton = ContentDialogButton.Primary
            };

            // Create a panel to hold both content and our custom buttons
            var mainContentPanel = new StackPanel 
            { 
                Orientation = System.Windows.Controls.Orientation.Vertical 
            };

            mainContentPanel.Children.Add(new System.Windows.Controls.TextBlock 
            { 
                Text = message, 
                TextWrapping = System.Windows.TextWrapping.Wrap,
                MaxWidth = 400,
                Margin = new System.Windows.Thickness(0, 0, 0, 20)
            });

            // Custom Button Row
            var buttonRow = new StackPanel 
            { 
                Orientation = System.Windows.Controls.Orientation.Horizontal, 
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            ContentDialogResult result = ContentDialogResult.None;

            if (!string.IsNullOrEmpty(primaryButtonText))
            {
                var btn = new Wpf.Ui.Controls.Button
                { 
                    Content = primaryButtonText, 
                    Appearance = ControlAppearance.Primary,
                    Margin = new System.Windows.Thickness(8, 0, 0, 0),
                    MinWidth = 80
                };
                btn.Click += (s, e) => { result = ContentDialogResult.Primary; dialog.Hide(); };
                buttonRow.Children.Add(btn);

                // Emulate Default Button behavior if needed (binding Enter key is harder here, but usually Primary Appearance helps)
            }

            if (!string.IsNullOrEmpty(secondaryButtonText))
            {
                var btn = new Wpf.Ui.Controls.Button
                { 
                    Content = secondaryButtonText,
                    Appearance = ControlAppearance.Secondary,
                    Margin = new System.Windows.Thickness(8, 0, 0, 0),
                    MinWidth = 80
                };
                btn.Click += (s, e) => { result = ContentDialogResult.Secondary; dialog.Hide(); };
                buttonRow.Children.Add(btn);
            }

            if (!string.IsNullOrEmpty(closeButtonText))
            {
                var btn = new Wpf.Ui.Controls.Button
                { 
                    Content = closeButtonText,
                    Appearance = ControlAppearance.Secondary,
                    Margin = new System.Windows.Thickness(8, 0, 0, 0),
                    MinWidth = 80
                };
                btn.Click += (s, e) => { result = ContentDialogResult.None; dialog.Hide(); };
                buttonRow.Children.Add(btn);
            }

            mainContentPanel.Children.Add(buttonRow);
            dialog.Content = mainContentPanel;

            await dialog.ShowAsync();
            return result;
        }

        /// <summary>
        /// Shows a confirmation dialog (Yes/No). Returns true if user clicked Yes.
        /// </summary>
        public static async Task<bool> ConfirmAsync(string title, string message)
        {
            var result = await ShowAsync(title, message, "Yes", "No");
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// Shows an alert dialog (OK only).
        /// </summary>
        public static async Task AlertAsync(string title, string message)
        {
            await ShowAsync(title, message, "OK");
        }

        /// <summary>
        /// Shows an error dialog (OK only).
        /// </summary>
        public static async Task ErrorAsync(string title, string message)
        {
            await ShowAsync(title, message, "OK");
        }

        /// <summary>
        /// Shows a warning dialog (OK only).
        /// </summary>
        public static async Task WarningAsync(string title, string message)
        {
            await ShowAsync(title, message, "OK");
        }
    }
}
