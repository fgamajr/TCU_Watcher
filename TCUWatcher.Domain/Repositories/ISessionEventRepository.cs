using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Domain.Entities;

namespace TCUWatcher.Domain.Repositories
{
    public interface ISessionEventRepository
    {
        Task<SessionEvent?> GetByIdAsync(string id);
        Task<SessionEvent?> GetBySourceIdAsync(string sourceId);
        Task<IEnumerable<SessionEvent>> GetAllAsync();
        Task<IEnumerable<SessionEvent>> GetActiveAsync();
        Task AddAsync(SessionEvent sessionEvent);
        Task UpdateAsync(SessionEvent sessionEvent);
        Task DeleteAsync(string id);
        Task<IEnumerable<SessionEvent>> GetSessionsInTimeRangeAsync(DateTime start, DateTime end);

    }
}
