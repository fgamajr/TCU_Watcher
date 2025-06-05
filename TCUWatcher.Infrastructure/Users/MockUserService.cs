using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Application.Users;
using TCUWatcher.Application.Users.DTOs;

namespace TCUWatcher.Infrastructure.Users
{
    public class MockUserService : IUserService
    {
        public Task<UserDto?> GetByEmailAsync(string email)
            => Task.FromResult<UserDto?>(new UserDto {
                Id = "mock-123",
                Name = "Usuário Mock",
                Email = email,
                Roles = new List<string> { "User" }
            });

        public Task CreateAsync(CreateUserDto user)
            => Task.CompletedTask;
    }
}
