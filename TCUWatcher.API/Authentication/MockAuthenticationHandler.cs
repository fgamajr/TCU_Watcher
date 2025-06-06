using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TCUWatcher.Application.Users;
using Microsoft.AspNetCore.Hosting.Server; // ISystemClock está aqui ou em Microsoft.AspNetCore.Authentication

namespace TCUWatcher.API.Authentication
{
    /// <summary>
    /// Handler mock que valida um token específico e rejeita os outros.
    /// </summary>
    public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ICurrentUserProvider _currentUserProvider;

        public MockAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ICurrentUserProvider currentUserProvider)
            : base(options, logger, encoder, clock)
        {
            _currentUserProvider = currentUserProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Cabeçalho Authorization ausente");

            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return AuthenticateResult.Fail("Cabeçalho Authorization inválido");

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.Fail("Token vazio");

            // ===================================================================
            // >> CORREÇÃO DE SEGURANÇA CRÍTICA <<
            // Rejeita qualquer token que não seja o token mock padrão.
            // ===================================================================
            if (token != "mock-token-abc123")
            {
                return AuthenticateResult.Fail("Token inválido.");
            }
            // ===================================================================

            var userDto = await _currentUserProvider.GetCurrentUserAsync();
            if (userDto is null)
                return AuthenticateResult.Fail("Usuário não encontrado no provider");

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