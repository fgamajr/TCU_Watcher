using Microsoft.AspNetCore.Mvc;
using TCUWatcher.Domain.Services;
using TCUWatcher.Infrastructure.Helpers;
using TCUWatcher.Infrastructure.Services;


namespace TCUWatcher.API.Controllers;

[ApiController]
[Route("api/validation")]
public class ValidationController : ControllerBase
{
    private readonly ITitleValidationService _titleValidationService;

    public ValidationController(ITitleValidationService titleValidationService)
    {
        _titleValidationService = titleValidationService;
    }

    [HttpPost("title")]
    public IActionResult CheckTitle([FromBody] string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("O título não pode estar vazio.");

        var isRelevant = _titleValidationService.IsRelevant(title);
        return Ok(new { title, isRelevant });
    }
}
