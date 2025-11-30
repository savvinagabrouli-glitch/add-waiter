using System.Collections.Generic;
using System.Threading.Tasks;
using Cursework.Domains.Models;

namespace Cursework.Application.Interfaces
{
    public interface IMenuService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Dish>> GetDishesAsync();
        Task<List<CategoryIngredient>> GetCategoryIngredientsAsync();
        Task<List<Ingredient>> GetIngredientsAsync();
        Task<List<DishIngredient>> GetDishIngredientsAsync();

        Task<List<Dish>> GetDishesByCategoryAsync(int categoryId);
        Task<List<Ingredient>> GetIngredientsByCategoryAsync(int categoryIngredientId);

        Task<Category> CreateCategoryAsync(Category item);
        Task UpdateCategoryAsync(Category item);
        Task DeleteCategoryAsync(int id);

        Task<Dish> CreateDishAsync(Dish item);
        Task UpdateDishAsync(Dish item);
        Task DeleteDishAsync(int id);

        Task<CategoryIngredient> CreateCategoryIngredientAsync(CategoryIngredient item);
        Task UpdateCategoryIngredientAsync(CategoryIngredient item);
        Task DeleteCategoryIngredientAsync(int id);

        Task<Ingredient> CreateIngredientAsync(Ingredient item);
        Task UpdateIngredientAsync(Ingredient item);
        Task DeleteIngredientAsync(int id);

        Task<DishIngredient> CreateDishIngredientAsync(DishIngredient item);
        Task UpdateDishIngredientAsync(DishIngredient item);
        Task DeleteDishIngredientAsync(int dishId, int ingredientId);
    }
}
