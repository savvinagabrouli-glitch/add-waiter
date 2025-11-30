using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/menu/ingredient-categories")]
public class IngredientCategoriesController : ControllerBase
{
    private readonly IMenuService _menu;

    public IngredientCategoriesController(IMenuService menu) => _menu = menu;

    [HttpGet]
    public async Task<ActionResult<List<CategoryIngredient>>> GetAll()
        => Ok(await _menu.GetCategoryIngredientsAsync());

    [HttpPost]
    public async Task<ActionResult<CategoryIngredient>> Create([FromBody] CategoryIngredient item)
    {
        try
        {
            var created = await _menu.CreateCategoryIngredientAsync(item);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryIngredient item)
    {
        if (id != item.Id)
            return BadRequest(new { message = "id в URL не совпадает с моделью" });

        try
        {
            await _menu.UpdateCategoryIngredientAsync(item);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _menu.DeleteCategoryIngredientAsync(id);
            return NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
