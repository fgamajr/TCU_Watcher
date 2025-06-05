using TCUWatcher.Domain.Entities;

namespace TCUWatcher.Domain.Repositories;

public interface IJudgedProcessRepository
{
    Task<IEnumerable<JudgedProcess>> GetByLiveEventIdAsync(string liveEventId);
    Task AddAsync(JudgedProcess process);
    Task UpdateAsync(JudgedProcess process);
}
