using TCUWatcher.Application.Users;
using TCUWatcher.Application.Users.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace TCUWatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;

    public AuthController(
        IAuthenticationService authenticationService,
        IUserService userService)
    {
        _authenticationService = authenticationService;
        _userService = userService;
    }

    // DTO de entrada para login
    public class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    // DTO de resposta ao cliente
    public class LoginResponse
    {
        public string Token { get; set; } = default!;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1) Validar se existe usuário (mockado)
        var userDto = await _userService.GetByEmailAsync(request.Email);
        if (userDto is null)
        {
            return Unauthorized(new { message = "Usuário não encontrado" });
        }

        // 2) Validar credenciais (sempre retorna true no mock)
        var valid = await _authenticationService.ValidateCredentialsAsync(request.Email, request.Password);
        if (!valid)
        {
            return Unauthorized(new { message = "Credenciais inválidas" });
        }

        // 3) Gerar “token” (mockado)
        var token = await _authenticationService.GenerateTokenAsync(userDto);

        return Ok(new LoginResponse { Token = token });
    }
}
