using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/menu/categories")]
public class MenuCategoriesController : ControllerBase
{
    private readonly IMenuService _menu;

    public MenuCategoriesController(IMenuService menu) => _menu = menu;

    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetAll()
        => Ok(await _menu.GetCategoriesAsync());

    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] Category category)
    {
        try
        {
            var created = await _menu.CreateCategoryAsync(category);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Category category)
    {
        if (id != category.Id)
            return BadRequest(new { message = "id в URl не совпадает с моделью" });

        try
        {
            await _menu.UpdateCategoryAsync(category);
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
            await _menu.DeleteCategoryAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
