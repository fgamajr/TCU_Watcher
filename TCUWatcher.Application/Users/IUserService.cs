using TCUWatcher.Application.Users.DTOs;

namespace TCUWatcher.Application.Users;

public interface IUserService
{
    Task<UserDto?> GetByEmailAsync(string email);
    Task CreateAsync(CreateUserDto user);
}
