using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Repositories;

namespace TCUWatcher.Infrastructure.SessionEvents
{
    /// <summary>
    /// Armazena SessionEvent em memória via ConcurrentDictionary.
    /// </summary>
    public class MockSessionEventRepository : ISessionEventRepository
    {
        private static readonly ConcurrentDictionary<string, SessionEvent> _storage 
            = new ConcurrentDictionary<string, SessionEvent>();

        public Task AddAsync(SessionEvent sessionEvent)
        {
            if (sessionEvent == null) throw new ArgumentNullException(nameof(sessionEvent));
            _storage[sessionEvent.Id] = sessionEvent;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            _storage.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SessionEvent>> GetActiveAsync()
        {
            var active = _storage.Values.Where(e => e.IsActive);
            return Task.FromResult(active);
        }

        public Task<IEnumerable<SessionEvent>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<SessionEvent>>(_storage.Values.ToList());
        }

        public Task<SessionEvent?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            _storage.TryGetValue(id, out var ev);
            return Task.FromResult(ev);
        }

        public Task<SessionEvent?> GetBySourceIdAsync(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentNullException(nameof(sourceId));
            var ev = _storage.Values.FirstOrDefault(x => x.SourceId == sourceId);
            return Task.FromResult(ev);
        }

        public Task UpdateAsync(SessionEvent sessionEvent)
        {
            if (sessionEvent == null) throw new ArgumentNullException(nameof(sessionEvent));
            _storage[sessionEvent.Id] = sessionEvent;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<SessionEvent>> GetSessionsInTimeRangeAsync(DateTime start, DateTime end)
        {
            var sessions = _storage.Values
                .Where(s => s.StartedAt.HasValue &&
                            s.StartedAt.Value >= start &&
                            s.StartedAt.Value <= end)
                .ToList();

            return Task.FromResult<IEnumerable<SessionEvent>>(sessions);
        }

    }
}