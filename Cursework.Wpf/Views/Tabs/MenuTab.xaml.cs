using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Cursework.Wpf.ViewModels.Admin;
using Cursework.Wpf.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Cursework.Wpf.Views.Tabs
{
    public partial class MenuTab : UserControl
    {
        public MenuTab()
        {
            InitializeComponent();
        }

        protected override async void OnInitialized(System.EventArgs e)
        {
            base.OnInitialized(e);
            // даём табу свой VM из DI
            var vm = App.Services.GetRequiredService<MenuTabViewModel>();
            DataContext = vm;
            await vm.LoadAsync();
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MenuTabViewModel;
            if (vm == null)
                return;

            if (vm.SelectedDish == null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Все файлы|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var dish = vm.SelectedDish;
                dish.PhotoUrl = dialog.FileName;

                // Dish не реализует INotifyPropertyChanged, поэтому обновим привязку через SelectedDish
                vm.SelectedDish = null;
                vm.SelectedDish = dish;
            }
        }

        // ==== Категории блюд ====

        private async void CategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;

            var dlg = new CategoryEditDialog
            {
                Owner = Window.GetWindow(this),
                TitleText = "Новая категория блюда"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var service = App.Services.GetRequiredService<IMenuService>();
                    var created = await service.CreateCategoryAsync(new Category
                    {
                        Name = dlg.CategoryName.Trim()
                    });

                    await vm.LoadAsync();
                    vm.SelectedCategory = vm.Categories.FirstOrDefault(c => c.Id == created.Id);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Категории блюд",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при создании категории: {ex.Message}",
                        "Категории блюд",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void CategoryEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;
            if (vm.SelectedCategory == null)
            {
                MessageBox.Show("Выберите категорию для редактирования.",
                    "Категории блюд",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new CategoryEditDialog
            {
                Owner = Window.GetWindow(this),
                TitleText = "Редактирование категории блюда",
                CategoryName = vm.SelectedCategory.Name
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var selectedId = vm.SelectedCategory.Id;
                    var service = App.Services.GetRequiredService<IMenuService>();
                    await service.UpdateCategoryAsync(new Category
                    {
                        Id = vm.SelectedCategory.Id,
                        Name = dlg.CategoryName.Trim()
                    });

                    await vm.LoadAsync();
                    vm.SelectedCategory = vm.Categories.FirstOrDefault(c => c.Id == selectedId);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Категории блюд",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при создании категории: {ex.Message}",
                        "Категории блюд",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;
            vm.CategoryDeleteCommand?.Execute(null);
        }

        // ==== Категории ингредиентов ====

        private async void CatIngredientAdd_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;

            var dlg = new CategoryIngredientEditDialog
            {
                Owner = Window.GetWindow(this),
                TitleText = "Новая категория ингредиентов"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var service = App.Services.GetRequiredService<IMenuService>();
                    var created = await service.CreateCategoryIngredientAsync(new CategoryIngredient
                    {
                        Name = dlg.CategoryName.Trim()
                    });

                    await vm.LoadAsync();
                    vm.SelectedCategoryIngredient =
                        vm.CategoryIngredients.FirstOrDefault(c => c.Id == created.Id);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Категории ингредиентов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при создании категории ингредиентов: {ex.Message}",
                        "Категории ингредиентов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void CatIngredientEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;
            if (vm.SelectedCategoryIngredient == null)
            {
                MessageBox.Show("Выберите категорию ингредиентов для редактирования.",
                    "Категории ингредиентов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new CategoryIngredientEditDialog
            {
                Owner = Window.GetWindow(this),
                TitleText = "Редактирование категории ингредиентов",
                CategoryName = vm.SelectedCategoryIngredient.Name
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var selectedId = vm.SelectedCategoryIngredient.Id;
                    var service = App.Services.GetRequiredService<IMenuService>();
                    await service.UpdateCategoryIngredientAsync(new CategoryIngredient
                    {
                        Id = vm.SelectedCategoryIngredient.Id,
                        Name = dlg.CategoryName.Trim()
                    });

                    await vm.LoadAsync();
                    vm.SelectedCategoryIngredient =
                        vm.CategoryIngredients.FirstOrDefault(c => c.Id == selectedId);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Категории ингредиентов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при создании категории ингредиентов: {ex.Message}",
                        "Категории ингредиентов",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void CatIngredientDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;
            vm.CatIngDeleteCommand?.Execute(null);
        }

        // ==== Ингредиенты ====

        private async void IngredientAdd_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;

            if (!vm.CategoryIngredients.Any())
            {
                MessageBox.Show("Сначала создайте хотя бы одну категорию ингредиентов.",
                    "Ингредиенты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new IngredientEditDialog
            {
                Owner = Window.GetWindow(this),
                TitleText = "Новый ингредиент"
            };

            dlg.CategoryIngredients.Clear();
            foreach (var ci in vm.CategoryIngredients)
                dlg.CategoryIngredients.Add(ci);

            if (dlg.ShowDialog() == true && dlg.SelectedCategoryIngredient != null)
            {
                try
                {
                    var service = App.Services.GetRequiredService<IMenuService>();
                    var created = await service.CreateIngredientAsync(new Ingredient
                    {
                        Name = dlg.IngredientName.Trim(),
                        CategoryIngredientId = dlg.SelectedCategoryIngredient.Id
                    });

                    await vm.LoadAsync();
                    vm.SelectedIngredient =
                        vm.Ingredients.FirstOrDefault(i => i.Id == created.Id);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Ингредиенты",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при создании ингредиента: {ex.Message}",
                        "Ингредиенты",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void IngredientEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;
            if (vm.SelectedIngredient == null)
            {
                MessageBox.Show("Выберите ингредиент для редактирования.",
                    "Ингредиенты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var ingredient = vm.SelectedIngredient;
            var ingredientId = ingredient.Id;

            var dlg = new IngredientEditDialog
            {
                Owner = Window.GetWindow(this),
                TitleText = "Редактирование ингредиента",
                IngredientName = vm.SelectedIngredient.Name
            };

            dlg.CategoryIngredients.Clear();
            foreach (var ci in vm.CategoryIngredients)
                dlg.CategoryIngredients.Add(ci);

            dlg.SelectedCategoryIngredient =
                vm.CategoryIngredients.FirstOrDefault(c => c.Id == vm.SelectedIngredient.CategoryIngredientId);

            if (dlg.ShowDialog() == true && dlg.SelectedCategoryIngredient != null)
            {
                try
                {
                    var service = App.Services.GetRequiredService<IMenuService>();
                    await service.UpdateIngredientAsync(new Ingredient
                    {
                        Id = vm.SelectedIngredient.Id,
                        Name = dlg.IngredientName.Trim(),
                        CategoryIngredientId = dlg.SelectedCategoryIngredient.Id
                    });

                    await vm.LoadAsync();
                    vm.SelectedIngredient =
                        vm.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Ингредиенты",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при создании ингредиента: {ex.Message}",
                        "Ингредиенты",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // Состав блюда
        private async void DishComposition_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MenuTabViewModel vm) return;

            if (vm.SelectedDish == null)
            {
                MessageBox.Show("Сначала выберите блюдо.",
                    "Состав блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (vm.SelectedDish.Id == 0)
            {
                MessageBox.Show("Сначала сохраните блюдо, затем задайте его состав.",
                    "Состав блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dish = vm.SelectedDish;
            var dishId = dish.Id;

            // ❗ Берём полный список, а не фильтрованный
            var allIngredients = vm.AllIngredients.ToList();
            if (!allIngredients.Any())
            {
                MessageBox.Show("Нет доступных ингредиентов. Сначала добавьте ингредиенты.",
                    "Состав блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // существующий состав для этого блюда
            var existing = vm.DishIngredients
                .Where(di => di.DishId == dishId)
                .ToList();

            // готовим строки для диалога
            var rows = existing.Select(di => new DishIngredientRow
            {
                IngredientId = di.IngredientId,
                Quantity = di.Quantity,
                Unit = di.Unit,
                IsExisting = true
            });

            // ❗ передаём полный список ингредиентов и стартовые строки в конструктор
            var dlg = new DishCompositionDialog(allIngredients, rows)
            {
                Owner = Window.GetWindow(this),
                HeaderText = $"Состав блюда: {dish.Name}"
            };

            if (dlg.ShowDialog() == true)
            {
                var service = App.Services.GetRequiredService<IMenuService>();

                // исходный состав по IngredientId
                var originalByIngredient = existing.ToDictionary(x => x.IngredientId, x => x);

                var currentRows = dlg.Rows.ToList();
                var currentIds = currentRows.Select(r => r.IngredientId).ToHashSet();

                // 1) новые строки
                var toCreate = currentRows
                    .Where(r => !originalByIngredient.ContainsKey(r.IngredientId))
                    .ToList();

                foreach (var r in toCreate)
                {
                    var dto = new DishIngredient
                    {
                        DishId = dishId,
                        IngredientId = r.IngredientId,
                        Quantity = r.Quantity,
                        Unit = r.Unit
                    };
                    await service.CreateDishIngredientAsync(dto);
                }

                // 2) обновлённые строки
                var toUpdate = currentRows
                    .Where(r => originalByIngredient.ContainsKey(r.IngredientId))
                    .Where(r =>
                    {
                        var orig = originalByIngredient[r.IngredientId];
                        return orig.Quantity != r.Quantity || orig.Unit != r.Unit;
                    })
                    .ToList();

                foreach (var r in toUpdate)
                {
                    var dto = new DishIngredient
                    {
                        DishId = dishId,
                        IngredientId = r.IngredientId,
                        Quantity = r.Quantity,
                        Unit = r.Unit
                    };
                    await service.UpdateDishIngredientAsync(dto);
                }

                // 3) удалённые строки
                var deletedIds = originalByIngredient.Keys
                    .Where(id => !currentIds.Contains(id))
                    .ToList();

                foreach (var ingrId in deletedIds)
                {
                    await service.DeleteDishIngredientAsync(dishId, ingrId);
                }

                // обновляем VM и возвращаем выделение блюда
                await vm.LoadAsync();
                vm.SelectedDish = vm.Dishes.FirstOrDefault(d => d.Id == dishId);
            }
        }
    }
}
