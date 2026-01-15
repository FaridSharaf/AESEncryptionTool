using System.Windows;

namespace AESCryptoTool.Views
{
    public partial class ProfileNameDialog : Window
    {
        public string ProfileName { get; private set; } = string.Empty;
        private readonly System.Collections.Generic.HashSet<string> _existingNames;

        public ProfileNameDialog(System.Collections.Generic.IEnumerable<string> existingNames = null)
        {
            InitializeComponent();
            
            // Handle custom title bar Close button
            CommandBindings.Add(new System.Windows.Input.CommandBinding(System.Windows.SystemCommands.CloseWindowCommand, (s, e) => System.Windows.SystemCommands.CloseWindow(this)));

            _existingNames = existingNames != null 
                ? new System.Collections.Generic.HashSet<string>(existingNames, System.StringComparer.OrdinalIgnoreCase) 
                : new System.Collections.Generic.HashSet<string>();
            
            ProfileNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string input = ProfileNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                CustomMessageBox.Show("Please enter a profile name.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_existingNames.Contains(input))
            {
                CustomMessageBox.Show($"A profile named '{input}' already exists.\nPlease choose a different name.", "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileName = input;
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
