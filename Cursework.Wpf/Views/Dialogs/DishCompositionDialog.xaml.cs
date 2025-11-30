using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using Cursework.Domains.Models;
using Cursework.Wpf.ViewModels.Base;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class DishCompositionDialog : Window, INotifyPropertyChanged
    {
        private string _headerText = "Состав блюда";
        public string HeaderText
        {
            get => _headerText;
            set { _headerText = value; OnPropertyChanged(); }
        }

        // Все доступные ингредиенты для выбора
        public IReadOnlyList<Ingredient> AllIngredients { get; }
        public ObservableCollection<Ingredient> Ingredients { get; } = new();

        // Коды единиц измерения (храним в модели)
        public ObservableCollection<string> UnitCodes { get; } =
            new(new[] { "g", "kg", "ml", "l", "pcs" });

        // Строки состава текущего блюда
        public ObservableCollection<DishIngredientRow> Rows { get; } = new();

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }

        public DishCompositionDialog() : this(Array.Empty<Ingredient>(), Enumerable.Empty<DishIngredientRow>())
        {

        }

        public DishCompositionDialog(
            IReadOnlyList<Ingredient> allIngredients,
            IEnumerable<DishIngredientRow>? existingRows = null)
        {
            InitializeComponent();
            DataContext = this;

            AllIngredients = allIngredients ?? Array.Empty<Ingredient>();

            // заполняем строки, если пришли уже существующие
            Rows.Clear();
            if (existingRows != null)
            {
                foreach (var row in existingRows)
                    Rows.Add(row);
            }

            AddRowCommand = new RelayCommand(_ =>
            {
                Rows.Add(new DishIngredientRow
                {
                    IngredientId = 0,
                    Quantity = 0,
                    Unit = "g",
                    IsExisting = false
                });
            });

            RemoveRowCommand = new RelayCommand(rowObj =>
            {
                if (rowObj is DishIngredientRow row && Rows.Contains(row))
                    Rows.Remove(row);
            });
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // убираем полностью пустые строки
            var nonEmptyRows = Rows
                .Where(r => r.IngredientId > 0 ||
                            r.Quantity > 0 ||
                            !string.IsNullOrWhiteSpace(r.Unit))
                .ToList();

            Rows.Clear();
            foreach (var r in nonEmptyRows)
                Rows.Add(r);

            if (!Rows.Any())
            {
                MessageBox.Show(
                    "Добавьте хотя бы один ингредиент в состав блюда.",
                    "Состав блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // валидация
            var invalid = Rows.Any(r =>
                r.IngredientId <= 0 ||
                r.Quantity <= 0 ||
                string.IsNullOrWhiteSpace(r.Unit));
            if (invalid)
            {
                MessageBox.Show(
                    "Для каждого ингредиента укажите: название, количество (> 0) и единицу измерения.",
                    "Состав блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // дубли ингредиентов
            var dup = Rows.GroupBy(r => r.IngredientId)
                          .FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
            {
                MessageBox.Show(
                    "Один и тот же ингредиент не может встречаться более одного раза в составе.",
                    "Состав блюда",
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

    public class DishIngredientRow : INotifyPropertyChanged
    {
        private int _ingredientId;
        public int IngredientId
        {
            get => _ingredientId;
            set { _ingredientId = value; OnPropertyChanged(); }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        private string _unit = "g";
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public bool IsExisting { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class AddOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i) return i + 1;
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class UnitCodeToRuConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var code = value as string ?? "";
            return code switch
            {
                "g" => "г",
                "kg" => "кг",
                "ml" => "мл",
                "l" => "л",
                "pcs" => "шт",
                _ => code
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var title = value as string ?? "";
            return title switch
            {
                "г" => "g",
                "кг" => "kg",
                "мл" => "ml",
                "л" => "l",
                "шт" => "pcs",
                _ => title
            };
        }
    }
}
