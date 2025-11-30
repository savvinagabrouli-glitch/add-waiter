using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/menu/dishes")]
public class DishesController : ControllerBase
{
    private readonly IMenuService _menu;

    public DishesController(IMenuService menu) => _menu = menu;

    [HttpGet]
    public async Task<ActionResult<List<Dish>>> GetAll()
        => Ok(await _menu.GetDishesAsync());

    [HttpPost]
    public async Task<ActionResult<Dish>> Create([FromBody] Dish dish)
    {
        try
        {
            var created = await _menu.CreateDishAsync(dish);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Dish dish)
    {
        if (id != dish.Id)
            return BadRequest(new { message = "id в URL не совпадает с моделью" });

        try
        {
            await _menu.UpdateDishAsync(dish);
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
            await _menu.DeleteDishAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
