using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCUWatcher.Application.Users;

namespace TCUWatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ICurrentUserProvider _currentUserProvider;

    public TestController(ICurrentUserProvider currentUserProvider)
    {
        _currentUserProvider = currentUserProvider;
    }

    [HttpGet("me")]
    [Authorize]  // ← protege o endpoint com o esquema “Bearer mock” configurado no Program.cs
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _currentUserProvider.GetCurrentUserAsync();
        return Ok(user);
    }
}
