using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staff;

    public StaffController(IStaffService staff) => _staff = staff;

    // GET: api/staff
    [HttpGet]
    public async Task<ActionResult<List<Staff>>> GetAll(CancellationToken ct)
    {
        var items = await _staff.GetAllAsync(ct);
        return Ok(items);
    }

    // POST: api/staff
    [HttpPost]
    public async Task<ActionResult<Staff>> Create([FromBody] Staff staff, CancellationToken ct)
    {
        try
        {
            var created = await _staff.AddAsync(staff, ct);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/staff/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Staff staff, CancellationToken ct)
    {
        if (id != staff.Id)
            return BadRequest(new { message = "id в URL не совпадает с моделью" });

        try
        {
            await _staff.UpdateAsync(staff, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/staff/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _staff.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
