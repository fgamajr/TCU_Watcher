using System.Threading.Tasks;
using TCUWatcher.Application.Users;
using TCUWatcher.Application.Users.DTOs;

namespace TCUWatcher.API.Authentication
{
    public class MockAuthenticationService : IAuthenticationService
    {
        public Task<bool> ValidateCredentialsAsync(string email, string password)
            => Task.FromResult(true);

        public Task<string> GenerateTokenAsync(UserDto user)
            => Task.FromResult("mock-token-abc123");
    }
}
