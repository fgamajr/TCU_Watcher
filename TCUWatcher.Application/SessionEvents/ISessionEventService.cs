using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Application.SessionEvents.DTOs;

namespace TCUWatcher.Application.SessionEvents
{
    public interface ISessionEventService
    {
        Task<IEnumerable<SessionEventDto>> GetAllAsync();
        Task<SessionEventDto?> GetByIdAsync(string id);
        Task<SessionEventDto> CreateAsync(CreateSessionEventDto input);
        Task<SessionEventDto?> UpdateAsync(string id, UpdateSessionEventDto input);
        Task DeleteAsync(string id);

        // Novo método para tratar upload manual:
        Task<SessionEventDto> CreateWithUploadAsync(CreateSessionEventWithUploadDto input);
    }
}
