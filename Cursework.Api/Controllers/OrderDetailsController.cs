using Cursework.Application.Interfaces;
using Cursework.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderDetailsController : ControllerBase
    {
        private readonly IOrderDetailsService _detailsService;

        public OrderDetailsController(IOrderDetailsService detailsService)
        {
            _detailsService = detailsService;
        }

        // GET: api/orderdetails/{orderId}
        [HttpGet("{orderId:int}")]
        public async Task<ActionResult<OrderDetailsDto>> Get(int orderId, CancellationToken ct)
        {
            var dto = await _detailsService.GetAsync(orderId, ct);
            if (dto == null)
                return NotFound();

            return dto;
        }

        // POST: api/orderdetails
        [HttpPost]
        public async Task<ActionResult<OrderDetailsDto>> Save([FromBody] OrderDetailsDto dto, CancellationToken ct)
        {
            if (dto == null || dto.OrderId <= 0)
                return BadRequest("Не указан корректный OrderId.");

            var saved = await _detailsService.SaveAsync(dto, ct);
            return Ok(saved);
        }

        // DELETE: api/orderdetails/{orderId}
        [HttpDelete("{orderId:int}")]
        public async Task<IActionResult> Delete(int orderId, CancellationToken ct)
        {
            await _detailsService.DeleteAsync(orderId, ct);
            return NoContent();
        }

        // GET: api/orderdetails/{orderId}/receipt
        [HttpGet("{orderId:int}/receipt")]
        public async Task<IActionResult> GetReceipt(int orderId, CancellationToken ct)
        {
            var bytes = await _detailsService.GetReceiptPdfAsync(orderId, ct);
            var fileName = $"receipt_order_{orderId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(bytes, "application/pdf", fileName);
        }
    }
}
