using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCUWatcher.API.Models;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Common;
using TCUWatcher.Domain.Errors;
using TCUWatcher.Domain.Services;

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
            // Este método na interface não foi alterado para Result, então continua ok.
            var list = await _sessionEventService.GetAllAsync();
            return Ok(list);
        }

        // GET /api/SessionEvents/{id} - CORRIGIDO
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sessionEventService.GetByIdAsync(id);

            return result.Match(
                sessionEventDto => Ok(sessionEventDto),
                error => ToProblemResult(error)
            );
        }

        // POST /api/SessionEvents - CORRIGIDO
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateSessionEventDto dto)
        {
            var result = await _sessionEventService.CreateAsync(dto);

            return result.Match(
                success => CreatedAtAction(nameof(GetById), new { id = success.Id }, success),
                error => ToProblemResult(error)
            );
        }

        // PUT /api/SessionEvents/{id} - CORRIGIDO
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSessionEventDto dto)
        {
            var result = await _sessionEventService.UpdateAsync(id, dto);
            
            return result.Match(
                sessionEventDto => Ok(sessionEventDto),
                error => ToProblemResult(error)
            );
        }

        // DELETE /api/SessionEvents/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            // Este método na interface não foi alterado para Result, então continua ok.
            await _sessionEventService.DeleteAsync(id);
            return NoContent();
        }

        // POST /api/SessionEvents/upload
        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(500_000_000)]
        public async Task<IActionResult> Upload([FromForm] SessionEventsUploadModel model)
        {
            if (model.VideoFile == null || model.VideoFile.Length == 0)
            {
                return BadRequest("Envie um arquivo de vídeo válido.");
            }

            var storageKey = $"manual/{Guid.NewGuid():N}_{model.VideoFile.FileName}";
            using (var stream = model.VideoFile.OpenReadStream())
            {
                await _storageService.SaveAsync(storageKey, stream);
            }

            var createDto = new CreateSessionEventWithUploadDto
            {
                Title = model.Title,
                StorageKey = storageKey,
                StartedAt = model.StartedAt ?? DateTime.UtcNow
            };

            // A chamada ao serviço agora retorna um 'Result'
            var result = await _sessionEventService.CreateWithUploadAsync(createDto);

            // E usamos o .Match() para tratar a resposta corretamente
            return result.Match(
                createdDto => CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto),
                error => ToProblemResult(error)
            );
        }
        
        private IActionResult ToProblemResult(DomainError error)
        {
            var statusCode = error switch
            {
                NotFoundError => StatusCodes.Status404NotFound,
                ValidationError => StatusCodes.Status400BadRequest,
                // Os erros que você já tinha:
                InvalidProcessNumberError => StatusCodes.Status400BadRequest,
                LiveAlreadyEndedError => 422, // Unprocessable Entity
                _ => StatusCodes.Status500InternalServerError
            };

            var problemDetails = new ProblemDetails
            {
                Title = error.Code,
                Detail = error.Message,
                Status = statusCode
            };
            
            return new ObjectResult(problemDetails);
        }
    }
}