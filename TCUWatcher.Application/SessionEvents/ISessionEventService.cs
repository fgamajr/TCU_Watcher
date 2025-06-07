using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Common;
using TCUWatcher.Domain.Errors;

namespace TCUWatcher.Application.SessionEvents
{
    public interface ISessionEventService
    {
        Task<IEnumerable<SessionEventDto>> GetAllAsync();
        Task<Result<SessionEventDto, DomainError>> GetByIdAsync(string id); // <-- Alterado
        Task<Result<SessionEventDto, DomainError>> CreateAsync(CreateSessionEventDto dto);
        Task<Result<SessionEventDto, DomainError>> UpdateAsync(string id, UpdateSessionEventDto input); // <-- Alterado
        Task<Result<bool, DomainError>> DeleteAsync(string id); // <-- Alterado para também retornar um Result

        Task<Result<SessionEventDto, DomainError>> CreateWithUploadAsync(CreateSessionEventWithUploadDto input); // <-- Alterado
    }
}