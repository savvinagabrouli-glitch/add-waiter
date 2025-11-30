using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/menu/ingredients")]
public class IngredientsController : ControllerBase
{
    private readonly IMenuService _menu;

    public IngredientsController(IMenuService menu)
    {
        _menu = menu;
    }

    [HttpGet]
    public async Task<ActionResult<List<Ingredient>>> GetAll()
        => Ok(await _menu.GetIngredientsAsync());

    [HttpPost]
    public async Task<ActionResult<Ingredient>> Create([FromBody] Ingredient item)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ",
                ModelState.Values.SelectMany(v => v.Errors)
                                 .Select(e => e.ErrorMessage));

            return BadRequest(new { message = errors });
        }

        try
        {
            var created = await _menu.CreateIngredientAsync(item);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Ingredient item)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ",
                ModelState.Values.SelectMany(v => v.Errors)
                                 .Select(e => e.ErrorMessage));

            return BadRequest(new { message = errors });
        }

        if (id != item.Id)
            return BadRequest(new { message = "id в URL не совпадает с моделью" });

        try
        {
            await _menu.UpdateIngredientAsync(item);
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
            await _menu.DeleteIngredientAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
