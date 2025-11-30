using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Cursework.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Cursework.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly AppDbContext _db;
        public MenuService(AppDbContext db) => _db = db;
        public async Task<List<Category>> GetCategoriesAsync() =>
            await _db.Categories
                     .AsNoTracking()
                     .OrderBy(x => x.Name)
                     .ToListAsync();

        public async Task<List<Dish>> GetDishesAsync() =>
            await _db.Dishes
                     .AsNoTracking()
                     .OrderBy(x => x.Name)
                     .ToListAsync();

        public async Task<List<CategoryIngredient>> GetCategoryIngredientsAsync() =>
            await _db.CategoryIngredients
                     .AsNoTracking()
                     .OrderBy(x => x.Name)
                     .ToListAsync();

        public async Task<List<Ingredient>> GetIngredientsAsync() =>
            await _db.Ingredients
                     .AsNoTracking()
                     .OrderBy(x => x.Name)
                     .ToListAsync();

        public async Task<List<DishIngredient>> GetDishIngredientsAsync() =>
            await _db.DishIngredients
                     .Include(di => di.Ingredient)
                     .AsNoTracking()
                     .OrderBy(x => x.DishId).ThenBy(x => x.IngredientId)
                     .ToListAsync();

        public async Task<List<Dish>> GetDishesByCategoryAsync(int categoryId) =>
            await _db.Dishes
                     .Where(d => d.CategoryId == categoryId)
                     .AsNoTracking()
                     .OrderBy(x => x.Name)
                     .ToListAsync();

        public async Task<List<Ingredient>> GetIngredientsByCategoryAsync(int categoryIngredientId) =>
            await _db.Ingredients
                     .Where(i => i.CategoryIngredientId == categoryIngredientId)
                     .AsNoTracking()
                     .OrderBy(x => x.Name)
                     .ToListAsync();
        public async Task<Category> CreateCategoryAsync(Category item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название категории обязательно.");

            var exists = await _db.Categories
                .AnyAsync(c => c.Name == item.Name);
            if (exists)
                throw new InvalidOperationException("Категория с таким названием уже существует.");

            _db.Categories.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateCategoryAsync(Category item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название категории обязательно.");

            var exists = await _db.Categories
                .AnyAsync(c => c.Name == item.Name && c.Id != item.Id);
            if (exists)
                throw new InvalidOperationException("Категория с таким названием уже существует.");

            _db.Categories.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var hasDishes = await _db.Dishes.AnyAsync(d => d.CategoryId == id);
            if (hasDishes)
                throw new InvalidOperationException(
                    "Нельзя удалить категорию, в которой есть блюда. " +
                    "Сначала перенесите блюда в другую категорию или удалите их.");

            var e = await _db.Categories.FindAsync(id);
            if (e != null)
            {
                _db.Categories.Remove(e);
                await _db.SaveChangesAsync();
            }
        }
        public async Task<Dish> CreateDishAsync(Dish item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название блюда обязательно.");

            if (item.Price <= 0)
                throw new InvalidOperationException("Цена блюда должна быть больше 0.");

            if (item.CategoryId <= 0)
                throw new InvalidOperationException("Выберите категорию блюда.");

            var exists = await _db.Dishes
                .AnyAsync(d => d.CategoryId == item.CategoryId && d.Name == item.Name);
            if (exists)
                throw new InvalidOperationException("В этой категории уже есть блюдо с таким названием.");

            _db.Dishes.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateDishAsync(Dish item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название блюда обязательно.");

            if (item.Price <= 0)
                throw new InvalidOperationException("Цена блюда должна быть больше 0.");

            if (item.CategoryId <= 0)
                throw new InvalidOperationException("Выберите категорию блюда.");

            var exists = await _db.Dishes
                .AnyAsync(d => d.CategoryId == item.CategoryId && d.Name == item.Name && d.Id != item.Id);
            if (exists)
                throw new InvalidOperationException("В этой категории уже есть блюдо с таким названием.");

            _db.Dishes.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteDishAsync(int id)
        {
            var usedInOrders = await _db.OrderItems.AnyAsync(oi => oi.DishId == id);
            if (usedInOrders)
                throw new InvalidOperationException(
                    "Нельзя удалить блюдо, которое уже использовалось в заказах. " +
                    "Вы можете скрыть его из меню (например, флагом 'Не активно').");

            var e = await _db.Dishes.FindAsync(id);
            if (e != null)
            {
                _db.Dishes.Remove(e);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<CategoryIngredient> CreateCategoryIngredientAsync(CategoryIngredient item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название категории ингредиентов обязательно.");

            var exists = await _db.CategoryIngredients
                .AnyAsync(c => c.Name == item.Name && c.Id != item.Id);
            if (exists)
                throw new InvalidOperationException("Категория ингредиентов с таким названием уже существует.");

            _db.CategoryIngredients.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateCategoryIngredientAsync(CategoryIngredient item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название категории ингредиентов обязательно.");

            var exists = await _db.CategoryIngredients
                .AnyAsync(c => c.Name == item.Name && c.Id != item.Id);
            if (exists)
                throw new InvalidOperationException("Категория ингредиентов с таким названием уже существует.");

            _db.CategoryIngredients.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteCategoryIngredientAsync(int id)
        {
            var hasIngredients = await _db.Ingredients.AnyAsync(i => i.CategoryIngredientId == id);
            if (hasIngredients)
                throw new InvalidOperationException(
                    "Нельзя удалить категорию ингредиентов, в которой есть ингредиенты. " +
                    "Сначала перенесите ингредиенты в другую категорию или удалите их.");

            var e = await _db.CategoryIngredients.FindAsync(id);
            if (e != null)
            {
                _db.CategoryIngredients.Remove(e);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Ingredient> CreateIngredientAsync(Ingredient item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название ингредиента обязательно.");

            if (item.CategoryIngredientId <= 0)
                throw new InvalidOperationException("Выберите категорию ингредиента.");

            var exists = await _db.Ingredients
                .AnyAsync(i => i.Name == item.Name && i.Id != item.Id);
            if (exists)
                throw new InvalidOperationException("Ингредиент с таким названием уже существует.");

            _db.Ingredients.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateIngredientAsync(Ingredient item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new InvalidOperationException("Название ингредиента обязательно.");

            if (item.CategoryIngredientId <= 0)
                throw new InvalidOperationException("Выберите категорию ингредиента.");

            var exists = await _db.Ingredients
                .AnyAsync(i => i.Name == item.Name && i.Id != item.Id);
            if (exists)
                throw new InvalidOperationException("Ингредиент с таким названием уже существует.");

            _db.Ingredients.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteIngredientAsync(int id)
        {
            var usedInDishes = await _db.DishIngredients.AnyAsync(di => di.IngredientId == id);
            if (usedInDishes)
                throw new InvalidOperationException("Нельзя удалить ингредиент, который используется в составе блюд.");

            var e = await _db.Ingredients.FindAsync(id);
            if (e != null)
            {
                _db.Ingredients.Remove(e);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<DishIngredient> CreateDishIngredientAsync(DishIngredient item)
        {
            var existing = await _db.DishIngredients
                                    .FirstOrDefaultAsync(x => x.DishId == item.DishId &&
                                                              x.IngredientId == item.IngredientId);
            if (existing != null)
            {
                existing.Quantity = item.Quantity;
                await _db.SaveChangesAsync();
                return existing;
            }

            _db.DishIngredients.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateDishIngredientAsync(DishIngredient item)
        {
            var existing = await _db.DishIngredients
                                    .FirstOrDefaultAsync(x => x.DishId == item.DishId &&
                                                              x.IngredientId == item.IngredientId);
            if (existing == null)
                throw new KeyNotFoundException("Пара (DishId, IngredientId) не найдена.");

            existing.Quantity = item.Quantity;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteDishIngredientAsync(int dishId, int ingredientId)
        {
            var e = await _db.DishIngredients
                             .FirstOrDefaultAsync(x => x.DishId == dishId && x.IngredientId == ingredientId);
            if (e != null)
            {
                _db.DishIngredients.Remove(e);
                await _db.SaveChangesAsync();
            }
        }

        public Task DeleteDishIngredientAsync(int id) =>
            throw new NotSupportedException("Для DishIngredient используйте DeleteDishIngredientAsync(dishId, ingredientId).");
    }
}
