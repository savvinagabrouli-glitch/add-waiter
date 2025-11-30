using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Cursework.Domains.Models;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class IngredientEditDialog : Window, INotifyPropertyChanged
    {
        private string _titleText = "Ингредиент";
        public string TitleText
        {
            get => _titleText;
            set { _titleText = value; OnPropertyChanged(); }
        }

        private string _ingredientName = string.Empty;
        public string IngredientName
        {
            get => _ingredientName;
            set { _ingredientName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CategoryIngredient> CategoryIngredients { get; } = new();

        private CategoryIngredient? _selectedCategoryIngredient;
        public CategoryIngredient? SelectedCategoryIngredient
        {
            get => _selectedCategoryIngredient;
            set { _selectedCategoryIngredient = value; OnPropertyChanged(); }
        }

        public IngredientEditDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(IngredientName))
            {
                MessageBox.Show("Название ингредиента обязательно.",
                    "Ингредиент",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (SelectedCategoryIngredient == null)
            {
                MessageBox.Show("Выберите категорию ингредиента.",
                    "Ингредиент",
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
