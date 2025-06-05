using TCUWatcher.Application.LiveEvents.DTOs;

namespace TCUWatcher.Application.LiveEvents;

public interface ILiveEventService
{
    Task<IEnumerable<LiveEventDto>> GetAllAsync();
    Task<LiveEventDto?> GetByIdAsync(string id);
    Task<LiveEventDto> CreateAsync(CreateLiveEventDto input);
}
