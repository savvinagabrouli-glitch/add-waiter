using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cursework.Application.Services.Api
{
    public class ApiMenuService : IMenuService
    {
        private readonly HttpClient _http;

        private const string CategoriesUrl = "api/menu/categories";
        private const string DishesUrl = "api/menu/dishes";
        private const string CategoryIngredientsUrl = "api/menu/ingredient-categories";
        private const string IngredientsUrl = "api/menu/ingredients";
        private const string DishIngredientsUrl = "api/menu/dish-ingredients";

        public ApiMenuService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        // ===== READ =====

        public async Task<List<Category>> GetCategoriesAsync()
        {
            var result = await _http.GetFromJsonAsync<List<Category>>(CategoriesUrl);
            return result ?? new List<Category>();
        }

        public async Task<List<Dish>> GetDishesAsync()
        {
            var result = await _http.GetFromJsonAsync<List<Dish>>(DishesUrl);
            return result ?? new List<Dish>();
        }

        public async Task<List<CategoryIngredient>> GetCategoryIngredientsAsync()
        {
            var result = await _http.GetFromJsonAsync<List<CategoryIngredient>>(CategoryIngredientsUrl);
            return result ?? new List<CategoryIngredient>();
        }

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            var result = await _http.GetFromJsonAsync<List<Ingredient>>(IngredientsUrl);
            return result ?? new List<Ingredient>();
        }

        public async Task<List<DishIngredient>> GetDishIngredientsAsync()
        {
            var result = await _http.GetFromJsonAsync<List<DishIngredient>>(DishIngredientsUrl);
            return result ?? new List<DishIngredient>();
        }

        public async Task<List<Dish>> GetDishesByCategoryAsync(int categoryId)
        {
            var all = await GetDishesAsync();
            return all.FindAll(d => d.CategoryId == categoryId);
        }

        public async Task<List<Ingredient>> GetIngredientsByCategoryAsync(int categoryIngredientId)
        {
            var all = await GetIngredientsAsync();
            return all.FindAll(i => i.CategoryIngredientId == categoryIngredientId);
        }

        public async Task<Category> CreateCategoryAsync(Category item)
        {
            var response = await _http.PostAsJsonAsync(CategoriesUrl, item);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Category>();
                if (created == null)
                    throw new InvalidOperationException("Backend вернул пустой ответ при создании категории.");

                return created;
            }

            await HandleCategoryErrorAsync(response, "создании категории");
            return item;
        }

        public async Task UpdateCategoryAsync(Category item)
        {
            var response = await _http.PutAsJsonAsync($"{CategoriesUrl}/{item.Id}", item);

            if (response.IsSuccessStatusCode)
                return;

            await HandleCategoryErrorAsync(response, "обновлении категории");
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var response = await _http.DeleteAsync($"{CategoriesUrl}/{id}");

            if (response.IsSuccessStatusCode)
                return;

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var json = await response.Content.ReadAsStringAsync();

                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                }
                catch (JsonException)
                {
                    if (!string.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException(json);
                }

                throw new InvalidOperationException("Не удалось удалить категорию: сервер вернул BadRequest.");
            }

            throw new Exception($"Ошибка при удалении категории. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }

        public async Task<Dish> CreateDishAsync(Dish item)
        {
            var dto = new Dish
            {
                Name = item.Name,
                Price = item.Price,
                CategoryId = item.CategoryId,
                Description = item.Description,
                PhotoUrl = item.PhotoUrl,
            };

            var response = await _http.PostAsJsonAsync(DishesUrl, dto);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Dish>();
                if (created == null)
                    throw new InvalidOperationException("Backend вернул пустой ответ при создании блюда.");

                return created;
            }

            await HandleCategoryErrorAsync(response, "создании блюда");
            return item;
        }

        public async Task UpdateDishAsync(Dish item)
        {
            var dto = new Dish
            {
                Id = item.Id,
                Name = item.Name,
                Price = item.Price,
                CategoryId = item.CategoryId,
                Description = item.Description,
                PhotoUrl = item.PhotoUrl,
            };

            var response = await _http.PutAsJsonAsync($"{DishesUrl}/{dto.Id}", dto);

            if (response.IsSuccessStatusCode)
                return;

            await HandleCategoryErrorAsync(response, "обновлении блюда");
        }

        public async Task DeleteDishAsync(int id)
        {
            var response = await _http.DeleteAsync($"{DishesUrl}/{id}");

            if (response.IsSuccessStatusCode)
                return;

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var json = await response.Content.ReadAsStringAsync();

                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                }
                catch (JsonException)
                {
                    if (!string.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException(json);
                }

                throw new InvalidOperationException("Не удалось удалить блюдо: сервер вернул BadRequest.");
            }

            throw new Exception($"Ошибка при удалении блюда. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }

        public async Task<CategoryIngredient> CreateCategoryIngredientAsync(CategoryIngredient item)
        {
            var response = await _http.PostAsJsonAsync(CategoryIngredientsUrl, item);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<CategoryIngredient>();
                if (created == null)
                    throw new InvalidOperationException("Backend вернул пустой ответ при создании категории ингредиентов.");

                return created;
            }

            await HandleCategoryErrorAsync(response, "создании категории ингредиентов");
            return item;
        }

        public async Task UpdateCategoryIngredientAsync(CategoryIngredient item)
        {
            var response = await _http.PutAsJsonAsync($"{CategoryIngredientsUrl}/{item.Id}", item);

            if (response.IsSuccessStatusCode)
                return;

            await HandleCategoryErrorAsync(response, "обновлении категории ингредиентов");
        }

        public async Task DeleteCategoryIngredientAsync(int id)
        {
            var response = await _http.DeleteAsync($"{CategoryIngredientsUrl}/{id}");

            if (response.IsSuccessStatusCode)
                return;

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var json = await response.Content.ReadAsStringAsync();

                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        throw new InvalidOperationException(error.Message);
                    }
                }
                catch (JsonException)
                {
                    if (!string.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException(json);
                }

                throw new InvalidOperationException("Не удалось удалить категорию ингредиентов: сервер вернул BadRequest.");
            }

            throw new Exception($"Ошибка при удалении категории ингредиентов. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }

        public async Task<Ingredient> CreateIngredientAsync(Ingredient item)
        {
            var dto = new Ingredient
            {
                Name = item.Name,
                CategoryIngredientId = item.CategoryIngredientId,
            };

            var response = await _http.PostAsJsonAsync(IngredientsUrl, dto);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Ingredient>();
                if (created == null)
                    throw new InvalidOperationException("Backend вернул пустой ответ при создании ингредиента.");

                return created;
            }

            await HandleCategoryErrorAsync(response, "создании ингредиента");
            return item;
        }

        public async Task UpdateIngredientAsync(Ingredient item)
        {
            var dto = new Ingredient
            {
                Id = item.Id,
                Name = item.Name,
                CategoryIngredientId = item.CategoryIngredientId,
            };

            var response = await _http.PutAsJsonAsync($"{IngredientsUrl}/{dto.Id}", dto);

            if (response.IsSuccessStatusCode)
                return;

            await HandleCategoryErrorAsync(response, "обновлении ингредиента");
        }

        public async Task DeleteIngredientAsync(int id)
        {
            var response = await _http.DeleteAsync($"{IngredientsUrl}/{id}");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                var message = error?.Message ?? "Не удалось удалить ингредиент.";

                throw new InvalidOperationException(message);
            }

            response.EnsureSuccessStatusCode();
        }

        public async Task<DishIngredient> CreateDishIngredientAsync(DishIngredient item)
        {
            var response = await _http.PostAsJsonAsync(DishIngredientsUrl, item);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<DishIngredient>();
            if (created == null)
                throw new InvalidOperationException("Backend вернул пустой ответ при создании состава блюда.");

            return created;
        }

        public async Task UpdateDishIngredientAsync(DishIngredient item)
        {
            var response = await _http.PutAsJsonAsync(DishIngredientsUrl, item);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteDishIngredientAsync(int dishId, int ingredientId)
        {
            var response = await _http.DeleteAsync($"{DishIngredientsUrl}/{dishId}/{ingredientId}");
            response.EnsureSuccessStatusCode();
        }

        public class ErrorResponse
        {
            public string? Message { get; set; }
        }
        private async Task HandleCategoryErrorAsync(HttpResponseMessage response, string operation)
        {
            var json = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                        throw new InvalidOperationException(error.Message);
                }
                catch (JsonException)
                {
                    if (!string.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException(json);
                }

                throw new InvalidOperationException($"Ошибка при {operation}: сервер вернул BadRequest.");
            }

            throw new Exception(
                $"Ошибка при {operation}. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }
    }
}
