namespace TCUWatcher.Application.Users;

using TCUWatcher.Application.Users.DTOs;

public interface ICurrentUserProvider
{
    Task<UserDto?> GetCurrentUserAsync();
}
