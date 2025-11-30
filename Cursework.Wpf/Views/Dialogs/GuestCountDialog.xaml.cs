using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Cursework.Application;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class GuestCountDialog : Window, INotifyPropertyChanged
    {
        private int _guestCount = 1;

        public int GuestCount
        {
            get => _guestCount;
            set
            {
                if (value < 1) value = 1;
                if (_guestCount != value)
                {
                    _guestCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public GuestCountDialog(int initialCount = 1)
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;

            if (initialCount > 0)
                GuestCount = initialCount;

            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (GuestCount < 1)
            {
                MessageBox.Show(
                    "Количество гостей должно быть хотя бы 1.",
                    "Детали заказа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
