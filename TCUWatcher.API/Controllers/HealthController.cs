using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.API.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous] // <- Permite chamadas sem autenticação
public class HealthController : ControllerBase
{
    private readonly IMonitoringWindowService _monitoringService;

    public HealthController(IMonitoringWindowService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var isInside = await _monitoringService.IsCurrentlyInActiveWindowAsync();

        return Ok(new
        {
            utcNow = nowUtc,
            dayOfWeek = nowUtc.DayOfWeek.ToString(),
            insideMonitoringWindow = isInside
        });
    }
}
