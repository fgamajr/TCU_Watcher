using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Services;
using TCUWatcher.API.Models;       

namespace TCUWatcher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionEventsController : ControllerBase
    {
        private readonly ISessionEventService _sessionEventService;
        private readonly IStorageService _storageService;

        public SessionEventsController(
            ISessionEventService sessionEventService,
            IStorageService storageService)
        {
            _sessionEventService = sessionEventService;
            _storageService = storageService;
        }

        // GET /api/SessionEvents
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var list = await _sessionEventService.GetAllAsync();
            return Ok(list);
        }

        // GET /api/SessionEvents/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(string id)
        {
            var ev = await _sessionEventService.GetByIdAsync(id);
            if (ev == null) return NotFound();
            return Ok(ev);
        }

        // POST /api/SessionEvents (criação “pura” sem arquivo)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateSessionEventDto dto)
        {
            var created = await _sessionEventService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/SessionEvents/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSessionEventDto dto)
        {
            var updated = await _sessionEventService.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // DELETE /api/SessionEvents/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            await _sessionEventService.DeleteAsync(id);
            return NoContent();
        }

        // ────────────────────────────────────────────────────────────────────
        // POST /api/SessionEvents/upload   ← Endpoint novo para upload manual
        // ────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Recebe um arquivo de vídeo (multipart/form-data), salva no Storage,
        /// e cria um SessionEvent com SourceType=ManualUpload.
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(500_000_000)]
        public async Task<IActionResult> Upload([FromForm] SessionEventsUploadModel model)
        {
            if (model.VideoFile == null || model.VideoFile.Length == 0)
                return BadRequest("Envie um arquivo de vídeo válido.");

            var storageKey = $"manual/{Guid.NewGuid():N}_{model.VideoFile.FileName}";
            using (var stream = model.VideoFile.OpenReadStream())
            {
                await _storageService.SaveAsync(storageKey, stream);
            }

            // Agora mapeamos para o DTO de aplicação (sem VideoFile):
            var createDto = new CreateSessionEventWithUploadDto
            {
                Title = model.Title,
                StorageKey = storageKey,
                StartedAt = model.StartedAt ?? DateTime.UtcNow
            };

            var createdDto = await _sessionEventService.CreateWithUploadAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
        }

    }
}
