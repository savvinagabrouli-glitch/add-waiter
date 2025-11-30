using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Cursework.Wpf.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class MenuTabViewModel : ViewModelBase
    {
        private readonly IMenuService _service;

        private readonly List<Dish> _allDishes = new();
        private readonly List<Ingredient> _allIngredients = new();
        public IReadOnlyList<Ingredient> AllIngredients => _allIngredients;

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Dish> Dishes { get; } = new();
        public ObservableCollection<CategoryIngredient> CategoryIngredients { get; } = new();
        public ObservableCollection<Ingredient> Ingredients { get; } = new();
        public ObservableCollection<DishIngredient> DishIngredients { get; } = new();

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (Set(ref _selectedCategory, value))
                {
                    ApplyDishFilter();

                    Raise(nameof(CanCategoryEdit));
                    Raise(nameof(CanCategoryDelete));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private Dish? _selectedDish;
        public Dish? SelectedDish
        {
            get => _selectedDish;
            set
            {
                if (Set(ref _selectedDish, value))
                {
                    Raise(nameof(CanDishEdit));
                    Raise(nameof(CanDishDelete));
                    Raise(nameof(CanDishSave));
                    Raise(nameof(CanDishCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private CategoryIngredient? _selectedCategoryIngredient;
        public CategoryIngredient? SelectedCategoryIngredient
        {
            get => _selectedCategoryIngredient;
            set
            {
                if (Set(ref _selectedCategoryIngredient, value))
                {
                    ApplyIngredientFilter();

                    Raise(nameof(CanCatIngEdit));
                    Raise(nameof(CanCatIngDelete));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private Ingredient? _selectedIngredient;
        public Ingredient? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if (Set(ref _selectedIngredient, value))
                {
                    Raise(nameof(CanIngredientEdit));
                    Raise(nameof(CanIngredientDelete));
                    Raise(nameof(CanIngredientSave));
                    Raise(nameof(CanIngredientCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private DishIngredient? _selectedDishIngredient;
        public DishIngredient? SelectedDishIngredient
        {
            get => _selectedDishIngredient;
            set => Set(ref _selectedDishIngredient, value);
        }

        private bool _isEditingCategory;
        public bool IsEditingCategory
        {
            get => _isEditingCategory;
            set
            {
                if (Set(ref _isEditingCategory, value))
                {
                    Raise(nameof(CanCategoryAdd));
                    Raise(nameof(CanCategoryEdit));
                    Raise(nameof(CanCategoryDelete));
                    Raise(nameof(CanCategorySave));
                    Raise(nameof(CanCategoryCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool _isEditingCatIngredient;
        public bool IsEditingCatIngredient
        {
            get => _isEditingCatIngredient;
            set
            {
                if (Set(ref _isEditingCatIngredient, value))
                {
                    Raise(nameof(CanCatIngAdd));
                    Raise(nameof(CanCatIngEdit));
                    Raise(nameof(CanCatIngDelete));
                    Raise(nameof(CanCatIngSave));
                    Raise(nameof(CanCatIngCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool _isEditingDish;
        public bool IsEditingDish
        {
            get => _isEditingDish;
            set
            {
                if (Set(ref _isEditingDish, value))
                {
                    Raise(nameof(CanDishAdd));
                    Raise(nameof(CanDishEdit));
                    Raise(nameof(CanDishDelete));
                    Raise(nameof(CanDishSave));
                    Raise(nameof(CanDishCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool _isEditingIngredient;
        public bool IsEditingIngredient
        {
            get => _isEditingIngredient;
            set
            {
                if (Set(ref _isEditingIngredient, value))
                {
                    Raise(nameof(CanIngredientAdd));
                    Raise(nameof(CanIngredientEdit));
                    Raise(nameof(CanIngredientDelete));
                    Raise(nameof(CanIngredientSave));
                    Raise(nameof(CanIngredientCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private bool _isNewCategory;
        private bool _isNewCatIngredient;
        private bool _isNewDish;
        private bool _isNewIngredient;
        public bool CanCategoryAdd => !IsEditingCategory;
        public bool CanCategoryEdit => SelectedCategory != null && !IsEditingCategory;
        public bool CanCategoryDelete => SelectedCategory != null && !IsEditingCategory;
        public bool CanCategorySave => IsEditingCategory && SelectedCategory != null;
        public bool CanCategoryCancel => IsEditingCategory;
        public bool CanDishAdd => !IsEditingDish;
        public bool CanDishEdit => SelectedDish != null && !IsEditingDish;
        public bool CanDishDelete => SelectedDish != null && !IsEditingDish;
        public bool CanDishSave => IsEditingDish && SelectedDish != null;
        public bool CanDishCancel => IsEditingDish;
        public bool CanIngredientAdd => !IsEditingIngredient;
        public bool CanIngredientEdit => SelectedIngredient != null && !IsEditingIngredient;
        public bool CanIngredientDelete => SelectedIngredient != null && !IsEditingIngredient;
        public bool CanIngredientSave => IsEditingIngredient && SelectedIngredient != null;
        public bool CanIngredientCancel => IsEditingIngredient;
        public bool CanCatIngAdd => !IsEditingCatIngredient;
        public bool CanCatIngEdit => SelectedCategoryIngredient != null && !IsEditingCatIngredient;
        public bool CanCatIngDelete => SelectedCategoryIngredient != null && !IsEditingCatIngredient;
        public bool CanCatIngSave => IsEditingCatIngredient && SelectedCategoryIngredient != null;
        public bool CanCatIngCancel => IsEditingCatIngredient;
        public RelayCommand CategoryAddCommand { get; }
        public RelayCommand CategoryBeginEditCommand { get; }
        public RelayCommand CategorySaveCommand { get; }
        public RelayCommand CategoryCancelCommand { get; }
        public RelayCommand CategoryDeleteCommand { get; }
        public RelayCommand DishAddCommand { get; }
        public RelayCommand DishBeginEditCommand { get; }
        public RelayCommand DishSaveCommand { get; }
        public RelayCommand DishCancelCommand { get; }
        public RelayCommand DishDeleteCommand { get; }
        public RelayCommand DishShowAllCommand { get; }
        public RelayCommand IngredientAddCommand { get; }
        public RelayCommand IngredientBeginEditCommand { get; }
        public RelayCommand IngredientSaveCommand { get; }
        public RelayCommand IngredientCancelCommand { get; }
        public RelayCommand IngredientDeleteCommand { get; }
        public RelayCommand IngredientShowAllCommand { get; }
        public RelayCommand CatIngAddCommand { get; }
        public RelayCommand CatIngBeginEditCommand { get; }
        public RelayCommand CatIngSaveCommand { get; }
        public RelayCommand CatIngCancelCommand { get; }
        public RelayCommand CatIngDeleteCommand { get; }

        public MenuTabViewModel(IMenuService service)
        {
            _service = service;

            CategoryAddCommand = new RelayCommand(_ => CategoryAdd(), _ => CanCategoryAdd);
            CategoryBeginEditCommand = new RelayCommand(_ => CategoryBeginEdit(), _ => CanCategoryEdit);
            CategorySaveCommand = new RelayCommand(async _ => await CategorySaveAsync(), _ => CanCategorySave);
            CategoryCancelCommand = new RelayCommand(_ => CategoryCancel(), _ => CanCategoryCancel);
            CategoryDeleteCommand = new RelayCommand(async _ => await CategoryDeleteAsync(), _ => CanCategoryDelete);

            DishAddCommand = new RelayCommand(_ => DishAdd(), _ => CanDishAdd);
            DishBeginEditCommand = new RelayCommand(_ => DishBeginEdit(), _ => CanDishEdit);
            DishSaveCommand = new RelayCommand(async _ => await DishSaveAsync(), _ => CanDishSave);
            DishCancelCommand = new RelayCommand(_ => DishCancel(), _ => CanDishCancel);
            DishDeleteCommand = new RelayCommand(async _ => await DishDeleteAsync(), _ => CanDishDelete);
            DishShowAllCommand = new RelayCommand(_ => DishShowAll());

            IngredientAddCommand = new RelayCommand(_ => IngredientAdd(), _ => CanIngredientAdd);
            IngredientBeginEditCommand = new RelayCommand(_ => IngredientBeginEdit(), _ => CanIngredientEdit);
            IngredientSaveCommand = new RelayCommand(async _ => await IngredientSaveAsync(), _ => CanIngredientSave);
            IngredientCancelCommand = new RelayCommand(_ => IngredientCancel(), _ => CanIngredientCancel);
            IngredientDeleteCommand = new RelayCommand(async _ => await IngredientDeleteAsync(), _ => CanIngredientDelete);
            IngredientShowAllCommand = new RelayCommand(_ => IngredientShowAll());

            CatIngAddCommand = new RelayCommand(_ => CatIngAdd(), _ => CanCatIngAdd);
            CatIngBeginEditCommand = new RelayCommand(_ => CatIngBeginEdit(), _ => CanCatIngEdit);
            CatIngSaveCommand = new RelayCommand(async _ => await CatIngSaveAsync(), _ => CanCatIngSave);
            CatIngCancelCommand = new RelayCommand(_ => CatIngCancel(), _ => CanCatIngCancel);
            CatIngDeleteCommand = new RelayCommand(async _ => await CatIngDeleteAsync(), _ => CanCatIngDelete);
        }

        public async Task LoadAsync()
        {
            await LoadCategoriesAsync();
            await LoadCategoryIngredientsAsync();
            await LoadDishesAsync();
            await LoadIngredientsAsync();
            await LoadDishIngredientsAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();
            var data = await _service.GetCategoriesAsync();
            foreach (var c in data.OrderBy(c => c.Name))
                Categories.Add(c);

            AttachCategoriesToDishes();
        }

        private async Task LoadCategoryIngredientsAsync()
        {
            CategoryIngredients.Clear();
            var data = await _service.GetCategoryIngredientsAsync();
            foreach (var c in data.OrderBy(c => c.Name))
                CategoryIngredients.Add(c);

            AttachCategoriesToIngredients();
        }

        private async Task LoadDishesAsync()
        {
            _allDishes.Clear();
            Dishes.Clear();

            var data = await _service.GetDishesAsync();
            _allDishes.AddRange(data);

            AttachCategoriesToDishes();
            ApplyDishFilter();
        }

        private async Task LoadIngredientsAsync()
        {
            _allIngredients.Clear();
            Ingredients.Clear();

            var data = await _service.GetIngredientsAsync();
            _allIngredients.AddRange(data);

            AttachCategoriesToIngredients();
            ApplyIngredientFilter();

            Raise(nameof(AllIngredients));
        }

        private async Task LoadDishIngredientsAsync()
        {
            DishIngredients.Clear();
            var data = await _service.GetDishIngredientsAsync();
            foreach (var di in data)
                DishIngredients.Add(di);
        }

        private void AttachCategoriesToDishes()
        {
            if (!Categories.Any() || !_allDishes.Any())
                return;

            foreach (var d in _allDishes)
            {
                d.Category = Categories.FirstOrDefault(c => c.Id == d.CategoryId)!;
            }
        }

        private void AttachCategoriesToIngredients()
        {
            if (!CategoryIngredients.Any() || !_allIngredients.Any())
                return;

            foreach (var i in _allIngredients)
            {
                i.CategoryIngredient = CategoryIngredients.FirstOrDefault(c => c.Id == i.CategoryIngredientId)!;
            }
        }

        private void ApplyDishFilter()
        {
            Dishes.Clear();

            IEnumerable<Dish> src = _allDishes;

            if (SelectedCategory != null)
                src = src.Where(d => d.CategoryId == SelectedCategory.Id);

            foreach (var d in src.OrderBy(d => d.Name))
                Dishes.Add(d);
        }

        private void ApplyIngredientFilter()
        {
            Ingredients.Clear();

            IEnumerable<Ingredient> src = _allIngredients;

            if (SelectedCategoryIngredient != null)
                src = src.Where(i => i.CategoryIngredientId == SelectedCategoryIngredient.Id);

            foreach (var i in src.OrderBy(i => i.Name))
                Ingredients.Add(i);
        }

        private void DishShowAll()
        {
            SelectedCategory = null;
            ApplyDishFilter();
        }

        private void IngredientShowAll()
        {
            SelectedCategoryIngredient = null;
            ApplyIngredientFilter();
        }

        private void CategoryAdd()
        {
            if (Categories.Any(c => c.Id == 0))
            {
                MessageBox.Show(
                    "Сначала сохраните или отмените создание текущей новой категории.",
                    "Категории блюд",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var fresh = new Category { Name = string.Empty };
            Categories.Add(fresh);
            SelectedCategory = fresh;
            _isNewCategory = true;
            IsEditingCategory = true;
        }

        private void CategoryBeginEdit()
        {
            if (SelectedCategory == null) return;

            _isNewCategory = SelectedCategory.Id == 0;
            IsEditingCategory = true;
        }

        private async Task CategorySaveAsync()
        {
            if (SelectedCategory == null) return;

            var item = SelectedCategory;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                MessageBox.Show("Название категории обязательно.", "Категории блюд",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (item.Id == 0)
                {
                    var created = await _service.CreateCategoryAsync(item);

                    var index = Categories.IndexOf(item);
                    if (index >= 0)
                        Categories[index] = created;
                    SelectedCategory = created;
                }
                else
                {
                    await _service.UpdateCategoryAsync(item);
                }

                _isNewCategory = false;
                IsEditingCategory = false;

                await LoadCategoriesAsync();
                await LoadDishesAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message,
                    "Категории блюд",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении категории: {ex.Message}",
                    "Категории блюд", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CategoryCancel()
        {
            IsEditingCategory = false;
            _isNewCategory = false;

            _ = LoadCategoriesAsync();
        }

        private async Task CategoryDeleteAsync()
        {
            if (SelectedCategory == null || SelectedCategory.Id == 0) return;

            var name = string.IsNullOrWhiteSpace(SelectedCategory.Name)
                ? "эту категорию"
                : $"категорию «{SelectedCategory.Name}»";

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить {name}?",
                "Удаление категории",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var id = SelectedCategory.Id;
                await _service.DeleteCategoryAsync(id);

                var toRemove = Categories.FirstOrDefault(c => c.Id == id);
                if (toRemove != null)
                    Categories.Remove(toRemove);

                if (SelectedCategory?.Id == id)
                    SelectedCategory = null;

                await LoadDishesAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message,
                    "Категории блюд",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить категорию: {ex.Message}",
                    "Категории блюд", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CatIngAdd()
        {
            if (CategoryIngredients.Any(c => c.Id == 0))
            {
                MessageBox.Show(
                    "Сначала сохраните или отмените создание текущей новой категории ингредиентов.",
                    "Категории ингредиентов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var fresh = new CategoryIngredient { Name = string.Empty };
            CategoryIngredients.Add(fresh);
            SelectedCategoryIngredient = fresh;
            _isNewCatIngredient = true;
            IsEditingCatIngredient = true;
        }

        private void CatIngBeginEdit()
        {
            if (SelectedCategoryIngredient == null) return;

            _isNewCatIngredient = SelectedCategoryIngredient.Id == 0;
            IsEditingCatIngredient = true;
        }

        private async Task CatIngSaveAsync()
        {
            if (SelectedCategoryIngredient == null) return;

            var item = SelectedCategoryIngredient;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                MessageBox.Show("Название категории ингредиентов обязательно.",
                    "Категории ингредиентов",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (item.Id == 0)
                {
                    var created = await _service.CreateCategoryIngredientAsync(item);
                    var index = CategoryIngredients.IndexOf(item);
                    if (index >= 0)
                        CategoryIngredients[index] = created;
                    SelectedCategoryIngredient = created;
                }
                else
                {
                    await _service.UpdateCategoryIngredientAsync(item);
                }

                _isNewCatIngredient = false;
                IsEditingCatIngredient = false;

                await LoadCategoryIngredientsAsync();
                await LoadIngredientsAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message,
                    "Категории ингредиентов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении категории ингредиентов: {ex.Message}",
                    "Категории ингредиентов",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CatIngCancel()
        {
            IsEditingCatIngredient = false;
            _isNewCatIngredient = false;

            _ = LoadCategoryIngredientsAsync();
        }

        private async Task CatIngDeleteAsync()
        {
            if (SelectedCategoryIngredient == null || SelectedCategoryIngredient.Id == 0) return;

            var name = string.IsNullOrWhiteSpace(SelectedCategoryIngredient.Name)
                ? "эту категорию ингредиентов"
                : $"категорию ингредиентов «{SelectedCategoryIngredient.Name}»";

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить {name}?",
                "Удаление категории ингредиентов",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var id = SelectedCategoryIngredient.Id;
                await _service.DeleteCategoryIngredientAsync(id);

                var toRemove = CategoryIngredients.FirstOrDefault(c => c.Id == id);
                if (toRemove != null)
                    CategoryIngredients.Remove(toRemove);

                if (SelectedCategoryIngredient?.Id == id)
                    SelectedCategoryIngredient = null;

                await LoadIngredientsAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Не удалось удалить категорию ингредиентов: {ex.Message}",
                                    "Категории ингредиентов",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить категорию ингредиентов: {ex.Message}",
                    "Категории ингредиентов",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IngredientAdd()
        {
            if (_allIngredients.Any(i => i.Id == 0))
            {
                MessageBox.Show(
                    "Сначала сохраните или отмените создание текущего нового ингредиента.",
                    "Ингредиенты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var fresh = new Ingredient
            {
                Name = string.Empty,
                CategoryIngredientId = SelectedCategoryIngredient?.Id ?? 0
            };

            if (fresh.CategoryIngredientId > 0)
                fresh.CategoryIngredient = CategoryIngredients.FirstOrDefault(c => c.Id == fresh.CategoryIngredientId)!;

            _allIngredients.Add(fresh);
            ApplyIngredientFilter();

            SelectedIngredient = fresh;
            _isNewIngredient = true;
            IsEditingIngredient = true;
        }

        private void IngredientBeginEdit()
        {
            if (SelectedIngredient == null) return;

            _isNewIngredient = SelectedIngredient.Id == 0;
            IsEditingIngredient = true;
        }

        private async Task IngredientSaveAsync()
        {
            if (SelectedIngredient == null) return;

            var item = SelectedIngredient;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                MessageBox.Show("Название ингредиента обязательно.", "Ингредиенты",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (item.CategoryIngredientId <= 0)
            {
                MessageBox.Show("Выберите категорию ингредиента.", "Ингредиенты",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (item.Id == 0)
                {
                    var created = await _service.CreateIngredientAsync(item);

                    var idx = _allIngredients.IndexOf(item);
                    if (idx >= 0)
                        _allIngredients[idx] = created;

                    AttachCategoriesToIngredients();
                    ApplyIngredientFilter();

                    SelectedIngredient = _allIngredients.FirstOrDefault(i => i.Id == created.Id);
                }
                else
                {
                    await _service.UpdateIngredientAsync(item);
                }

                _isNewIngredient = false;
                IsEditingIngredient = false;

                await LoadIngredientsAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ингредиент",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении ингредиента: {ex.Message}",
                    "Ингредиенты",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IngredientCancel()
        {
            IsEditingIngredient = false;
            _isNewIngredient = false;

            _ = LoadIngredientsAsync();
        }

        private async Task IngredientDeleteAsync()
        {
            if (SelectedIngredient == null || SelectedIngredient.Id == 0) return;

            var name = string.IsNullOrWhiteSpace(SelectedIngredient.Name)
                ? "этот ингредиент"
                : $"ингредиент «{SelectedIngredient.Name}»";

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить {name}?",
                "Удаление ингредиента",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _service.DeleteIngredientAsync(SelectedIngredient.Id);

                _allIngredients.RemoveAll(i => i.Id == SelectedIngredient.Id);
                ApplyIngredientFilter();
                SelectedIngredient = null;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Удаление ингредиента",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (HttpRequestException)
            {
                MessageBox.Show(
                    "Нельзя удалить ингредиент: он используется в составе блюд.",
                    "Удаление ингредиента",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void DishAdd()
        {
            if (_allDishes.Any(d => d.Id == 0))
            {
                MessageBox.Show(
                    "Сначала сохраните или отмените создание текущего нового блюда.",
                    "Блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var fresh = new Dish
            {
                Name = string.Empty,
                Price = 0m,
                CategoryId = SelectedCategory?.Id ?? 0
            };

            if (fresh.CategoryId > 0)
                fresh.Category = Categories.FirstOrDefault(c => c.Id == fresh.CategoryId)!;

            _allDishes.Add(fresh);
            AttachCategoriesToDishes();
            ApplyDishFilter();

            SelectedDish = fresh;
            _isNewDish = true;
            IsEditingDish = true;
        }

        private void DishBeginEdit()
        {
            if (SelectedDish == null) return;

            _isNewDish = SelectedDish.Id == 0;
            IsEditingDish = true;
        }

        private void DishCancel()
        {
            IsEditingDish = false;
            _isNewDish = false;

            _ = LoadDishesAsync();
        }

        private async Task DishSaveAsync()
        {
            if (SelectedDish == null) return;

            var dish = SelectedDish;

            if (string.IsNullOrWhiteSpace(dish.Name))
            {
                MessageBox.Show("Название блюда обязательно.", "Блюда",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dish.Price <= 0)
            {
                MessageBox.Show("Цена блюда должна быть больше 0.", "Блюда",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dish.CategoryId <= 0)
            {
                MessageBox.Show("Выберите категорию блюда.", "Блюда",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (dish.Id == 0)
                {
                    var created = await _service.CreateDishAsync(dish);

                    var idx = _allDishes.IndexOf(dish);
                    if (idx >= 0)
                        _allDishes[idx] = created;

                    AttachCategoriesToDishes();
                    ApplyDishFilter();

                    SelectedDish = _allDishes.FirstOrDefault(d => d.Id == created.Id);
                }
                else
                {
                    await _service.UpdateDishAsync(dish);
                }

                _isNewDish = false;
                IsEditingDish = false;

                await LoadDishesAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message,
                    "Блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении блюда: {ex.Message}",
                    "Блюда",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DishDeleteAsync()
        {
            if (SelectedDish == null || SelectedDish.Id == 0) return;

            var name = string.IsNullOrWhiteSpace(SelectedDish.Name)
                ? "это блюдо"
                : $"блюдо «{SelectedDish.Name}»";

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить {name}?",
                "Удаление блюда",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var id = SelectedDish.Id;
                await _service.DeleteDishAsync(id);

                _allDishes.RemoveAll(d => d.Id == id);
                ApplyDishFilter();

                SelectedDish = null;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message,
                    "Блюда",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить блюдо: {ex.Message}",
                    "Блюда",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
