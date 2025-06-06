// TCUWatcher.Infrastructure/SessionEvents/MockSessionEventRepository.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Repositories;

namespace TCUWatcher.Infrastructure.SessionEvents
{
    public class MockSessionEventRepository : ISessionEventRepository
    {
        private static readonly List<SessionEvent> _events = new();

        public Task<SessionEvent?> GetByIdAsync(string id)
        {
            var evt = _events.FirstOrDefault(e => e.Id == id);
            return Task.FromResult<SessionEvent?>(evt);
        }

        public Task<SessionEvent?> GetBySourceIdAsync(string sourceId)
        {
            var evt = _events.FirstOrDefault(e => e.SourceId == sourceId);
            return Task.FromResult<SessionEvent?>(evt);
        }

        public Task<IEnumerable<SessionEvent>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<SessionEvent>>(_events);
        }

        public Task<IEnumerable<SessionEvent>> GetActiveAsync()
        {
            var active = _events.Where(e => e.IsLive && e.EndedAt == null);
            return Task.FromResult<IEnumerable<SessionEvent>>(active);
        }

        public Task AddAsync(SessionEvent sessionEvent)
        {
            _events.Add(sessionEvent);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SessionEvent sessionEvent)
        {
            var idx = _events.FindIndex(e => e.Id == sessionEvent.Id);
            if (idx >= 0) _events[idx] = sessionEvent;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            var idx = _events.FindIndex(e => e.Id == id);
            if (idx >= 0)
            {
                _events.RemoveAt(idx);
            }
            return Task.CompletedTask;
        }
    }
}
