using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAll(CancellationToken ct)
    {
        var list = await _orders.GetAllAsync(ct);
        return Ok(list);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        try
        {
            if (dto.TableId <= 0)
                return BadRequest(new { message = "Не указан стол для заказа." });

            if (string.IsNullOrWhiteSpace(dto.Status))
                dto.Status = "Preorder";

            if (dto.Status == "Preorder")
            {
                var hasActive = await _orders.HasActiveOrderForTableAsync(dto.TableId, ct);
                if (hasActive)
                {
                    return BadRequest(new
                    {
                        message = "Для этого стола уже есть активный заказ. " +
                                  "Предзаказ отправить нельзя."
                    });
                }
            }

            var order = new Order
            {
                TableId = dto.TableId,
                WaiterId = dto.WaiterId,
                Status = dto.Status
            };

            var created = await _orders.AddAsync(order, ct);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/orders/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Order order, CancellationToken ct)
    {
        if (id != order.Id)
            return BadRequest(new { message = "id в URL не совпадает с моделью" });

        try
        {
            await _orders.UpdateAsync(order, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/orders/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _orders.DeleteAsync(id, ct);
        return NoContent();
    }

    public class CreateOrderDto
    {
        public int TableId { get; set; }
        public string Status { get; set; } = "Preorder";
        public int? WaiterId { get; set; }
    }
}
