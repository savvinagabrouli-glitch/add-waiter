using Cursework.Application.Interfaces;
using Cursework.Application.Models;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/menu/dish-ingredients")]
public class DishIngredientsController : ControllerBase
{
    private readonly IMenuService _menu;

    public DishIngredientsController(IMenuService menu) => _menu = menu;

    [HttpGet]
    public async Task<ActionResult<List<DishIngredient>>> GetAll()
        => Ok(await _menu.GetDishIngredientsAsync());

    [HttpPost]
    public async Task<ActionResult<DishIngredient>> Create([FromBody] DishIngredient item)
    {
        var created = await _menu.CreateDishIngredientAsync(item);
        return CreatedAtAction(nameof(GetAll),
            new { dishId = created.DishId, ingredientId = created.IngredientId }, created);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] DishIngredient item)
    {
        await _menu.UpdateDishIngredientAsync(item);
        return NoContent();
    }

    [HttpDelete("{dishId:int}/{ingredientId:int}")]
    public async Task<IActionResult> Delete(int dishId, int ingredientId)
    {
        await _menu.DeleteDishIngredientAsync(dishId, ingredientId);
        return NoContent();
    }

    [HttpGet("{dishId:int}")]
    public async Task<ActionResult<List<DishIngredientDto>>> GetByDish(int dishId)
    {
        var all = await _menu.GetDishIngredientsAsync();

        var items = all
            .Where(di => di.DishId == dishId)
            .Select(di => new DishIngredientDto
            {
                IngredientId = di.IngredientId,
                IngredientName = di.Ingredient?.Name ?? string.Empty
            })
            .ToList();

        return Ok(items);
    }
}
