using TCUWatcher.Application.Users.DTOs;

namespace TCUWatcher.Application.Users;

public interface IAuthenticationService
{
    Task<string> GenerateTokenAsync(UserDto user);
    Task<bool> ValidateCredentialsAsync(string email, string password);
}
