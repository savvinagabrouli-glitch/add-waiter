using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cursework.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallWaiterController : ControllerBase
    {
        private readonly ICallWaiterService _callWaiters;

        public CallWaiterController(ICallWaiterService callWaiters)
        {
            _callWaiters = callWaiters;
        }

        // GET: api/callwaiter
        [HttpGet]
        public async Task<ActionResult<List<CallWaiter>>> GetAll(CancellationToken ct)
        {
            var items = await _callWaiters.GetAllAsync(ct);
            return Ok(items);
        }

        // GET: api/callwaiter/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CallWaiter>> Get(int id, CancellationToken ct)
        {
            var items = await _callWaiters.GetAllAsync(ct);
            var item = items.FirstOrDefault(c => c.Id == id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        // POST: api/callwaiter
        [HttpPost]
        public async Task<ActionResult<CallWaiter>> Create([FromBody] CallWaiter callWaiter, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _callWaiters.AddAsync(callWaiter, ct);

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        // PUT: api/callwaiter/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CallWaiter callWaiter, CancellationToken ct)
        {
            if (id != callWaiter.Id)
                return BadRequest("id в URL не совпадает с моделью");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _callWaiters.UpdateAsync(callWaiter, ct);
            return NoContent();
        }

        // DELETE: api/callwaiter/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _callWaiters.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
