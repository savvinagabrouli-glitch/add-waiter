using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class CategoryEditDialog : Window, INotifyPropertyChanged
    {
        private string _titleText = "Категория блюда";
        public string TitleText
        {
            get => _titleText;
            set { _titleText = value; OnPropertyChanged(); }
        }

        private string _categoryName = string.Empty;
        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        public CategoryEditDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                MessageBox.Show("Название категории обязательно.",
                    "Категория блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
