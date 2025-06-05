using TCUWatcher.Domain.Entities;

namespace TCUWatcher.Domain.Repositories;

public interface ILiveEventRepository
{
    Task<LiveEvent?> GetByIdAsync(string id);
    Task<LiveEvent?> GetBySourceIdAsync(string sourceId);
    Task<IEnumerable<LiveEvent>> GetAllAsync();
    Task<IEnumerable<LiveEvent>> GetActiveAsync();
    Task AddAsync(LiveEvent liveEvent);
    Task UpdateAsync(LiveEvent liveEvent);
}
