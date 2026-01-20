using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using AESCryptoTool.Services;

namespace AESCryptoTool
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            // Global exception handling
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize Theme
                var savedTheme = ThemeManager.LoadSavedTheme();
                ThemeManager.ApplyTheme(savedTheme);

                // Log startup
                Console.WriteLine("Application Started.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup Error: {ex.Message}\n\n{ex.StackTrace}", "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowError(e.Exception, "UI Error");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ShowError(ex, "System Error");
            }
        }

        private void ShowError(Exception ex, string title)
        {
            string message = $"An unexpected error occurred:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            
            // Attempt to log to file
            try
            {
                File.AppendAllText("crash.log", $"{DateTime.Now}: {message}\n-------------------\n");
            }
            catch { }
        }
    }
}
