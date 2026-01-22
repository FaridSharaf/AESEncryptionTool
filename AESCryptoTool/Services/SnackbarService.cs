using System;
using Wpf.Ui.Controls;

using System.Windows.Media;

namespace AESCryptoTool.Services
{
    public static class SnackbarService
    {
        private static SnackbarPresenter? _presenter;

        public static void Initialize(SnackbarPresenter presenter)
        {
            _presenter = presenter;
        }

        public static void Show(string title, string message, ControlAppearance appearance = ControlAppearance.Primary, SymbolRegular icon = SymbolRegular.Info24, int durationSeconds = 3)
        {
            if (_presenter == null) return;

            var snackbar = new Snackbar(_presenter)
            {
                Title = title,
                Content = message,
                Appearance = appearance,
                Icon = new SymbolIcon(icon),
                Timeout = TimeSpan.FromSeconds(durationSeconds)
            };
            
            snackbar.Show();
        }

        public static void Success(string message, string title = "Success")
        {
            Show(title, message, ControlAppearance.Success, SymbolRegular.CheckmarkCircle24);
        }

        public static void Information(string message, string title = "Information")
        {
            Show(title, message, ControlAppearance.Info, SymbolRegular.Info24);
        }

        public static void Warning(string message, string title = "Warning")
        {
            Show(title, message, ControlAppearance.Caution, SymbolRegular.Warning24);
        }

        public static void Error(string message, string title = "Error")
        {
            Show(title, message, ControlAppearance.Danger, SymbolRegular.ErrorCircle24, 5);
        }
    }
}
