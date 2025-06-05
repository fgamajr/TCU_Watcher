using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TCUWatcher.Application.Users;
using TCUWatcher.Application.Users.DTOs;
// using System; // You don't need this if sticking with ISystemClock

namespace TCUWatcher.API.Authentication
{
    /// <summary>
    /// Um AuthenticationHandler “mock” que aceita qualquer valor não-vazio em "Authorization: Bearer &lt;token&gt;"
    /// e considera o usuário retornado por ICurrentUserProvider como autenticado.
    /// </summary>
    public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ICurrentUserProvider _currentUserProvider;

        public MockAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock, // Keep ISystemClock here
            ICurrentUserProvider currentUserProvider)
            : base(options, logger, encoder, clock) // Pass clock here
        {
            _currentUserProvider = currentUserProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // ... (rest of your code remains the same)
            // 1) Verifica se existe header Authorization
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Cabeçalho Authorization ausente");

            // 2) Extrai valor completo "Bearer <token>"
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return AuthenticateResult.Fail("Cabeçalho Authorization inválido");

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.Fail("Token vazio");

            // 3) Recupera o usuário mock
            var userDto = await _currentUserProvider.GetCurrentUserAsync();
            if (userDto is null)
                return AuthenticateResult.Fail("Usuário não encontrado no provider");

            // 4) Monta ClaimsPrincipal a partir do UserDto
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userDto.Id),
                new Claim(ClaimTypes.Name, userDto.Name),
                new Claim(ClaimTypes.Email, userDto.Email)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            foreach (var role in userDto.Roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}