using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Application.Users;
using TCUWatcher.Application.Users.DTOs;

namespace TCUWatcher.Infrastructure.Users
{
    public class MockUserProvider : ICurrentUserProvider
    {
        public Task<UserDto?> GetCurrentUserAsync()
        {
            var mockUser = new UserDto
            {
                Id = "mock-user-001",
                Name = "Auditor TCU",
                Email = "auditor@tcu.gov.br",
                Roles = new List<string> { "Admin" }
            };

            return Task.FromResult<UserDto?>(mockUser);
        }
    }
}
