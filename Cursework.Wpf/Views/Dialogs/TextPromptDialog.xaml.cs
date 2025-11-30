using System.Windows;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class TextPromptDialog : Window
    {
        public string ResultText => InputBox.Text?.Trim() ?? "";

        public TextPromptDialog(string title, string message, string defaultValue)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
            InputBox.Text = defaultValue ?? "";
            InputBox.SelectAll();
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultText))
            {
                MessageBox.Show("Введите непустое имя.", "Пресет", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
