using System.Windows;

namespace ConsoleApp1.Views
{
    public partial class NoteEditDialog : Window
    {
        public string NoteText { get; private set; } = string.Empty;

        public NoteEditDialog(string currentNote = "")
        {
            InitializeComponent();
            NoteTextBox.Text = currentNote;
            NoteTextBox.Focus();
            NoteTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            NoteText = NoteTextBox.Text;
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

