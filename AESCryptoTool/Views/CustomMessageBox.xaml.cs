using System.Windows;
using System.Linq;

namespace AESCryptoTool.Views
{
    using Application = System.Windows.Application;
    
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBox(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();
            
            // Register window command bindings for custom title bar buttons
            CommandBindings.Add(new System.Windows.Input.CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
            
            Title = title;
            MessageText.Text = message;
            
            // Set Icon
            switch (icon)
            {
                case MessageBoxImage.Question: IconText.Text = "❓"; break;
                case MessageBoxImage.Error: IconText.Text = "❌"; IconText.Foreground = System.Windows.Media.Brushes.Red; break;
                case MessageBoxImage.Warning: IconText.Text = "⚠️"; IconText.Foreground = System.Windows.Media.Brushes.Orange; break;
                case MessageBoxImage.Information:
                default: IconText.Text = "ℹ️"; break; // Keep theme color for Info
            }

            // Set Buttons
            switch (button)
            {
                case MessageBoxButton.YesNo:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    YesButton.Focus();
                    break;
                case MessageBoxButton.OK:
                default:
                    OkButton.Visibility = Visibility.Visible;
                    OkButton.Focus();
                    break;
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            var msgBox = new CustomMessageBox(message, title, button, icon);
            
            // Set Owner to active window if possible
            if (Application.Current != null)
            {
                 var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
                 msgBox.Owner = activeWindow ?? Application.Current.MainWindow;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}
