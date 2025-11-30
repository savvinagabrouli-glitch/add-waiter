using System.Windows;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class ResetPasswordDialog : Window
    {
        public string? Password { get; private set; }

        public ResetPasswordDialog()
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var p1 = pwd1.Password;
            var p2 = pwd2.Password;

            if (string.IsNullOrWhiteSpace(p1) || p1.Length < 4)
            {
                MessageBox.Show(this, "Введите пароль (минимум 4 символа).", "Пароль", MessageBoxButton.OK, MessageBoxImage.Information);
                pwd1.Focus();
                return;
            }
            if (p1 != p2)
            {
                MessageBox.Show(this, "Пароли не совпадают.", "Пароль", MessageBoxButton.OK, MessageBoxImage.Information);
                pwd2.Focus();
                return;
            }

            Password = p1;
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
