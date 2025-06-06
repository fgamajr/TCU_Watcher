// TCUWatcher.API/Controllers/SessionEventsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;

namespace TCUWatcher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionEventsController : ControllerBase
    {
        private readonly ISessionEventService _sessionEventService;

        public SessionEventsController(ISessionEventService sessionEventService)
        {
            _sessionEventService = sessionEventService;
        }

        // GET /api/SessionEvents
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _sessionEventService.GetAllAsync();
            return Ok(items);
        }

        // GET /api/SessionEvents/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var evt = await _sessionEventService.GetByIdAsync(id);
            if (evt == null) return NotFound();
            return Ok(evt);
        }

        // POST /api/SessionEvents
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSessionEventDto input)
        {
            var created = await _sessionEventService.CreateAsync(input);
            return CreatedAtAction(nameof(GetById),
                                   new { id = created.Id },
                                   created);
        }

        // PUT /api/SessionEvents/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSessionEventDto input)
        {
            var updated = await _sessionEventService.UpdateAsync(id, input);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE /api/SessionEvents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _sessionEventService.DeleteAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
