using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Repositories;

namespace TCUWatcher.Infrastructure.Repositories
{
    public class MockSessionEventRepository : ISessionEventRepository
    {
        private readonly ConcurrentDictionary<Guid, SessionEvent> _sessionEvents = new();

        public Task<SessionEvent> CreateAsync(SessionEvent sessionEvent)
        {
            _sessionEvents[sessionEvent.Id] = sessionEvent;
            Console.WriteLine($"[MockRepo] CRIADO: {sessionEvent.Id} | {sessionEvent.Title}");
            return Task.FromResult(sessionEvent);
        }

        public Task<SessionEvent?> GetByIdAsync(Guid id)
        {
            _sessionEvents.TryGetValue(id, out var sessionEvent);
            Console.WriteLine($"[MockRepo] GET BY ID: {id} {(sessionEvent != null ? "→ OK" : "→ NÃO ENCONTRADO")}");
            return Task.FromResult(sessionEvent);
        }

        public Task<List<SessionEvent>> GetAllAsync()
        {
            Console.WriteLine($"[MockRepo] LISTANDO TODOS: {_sessionEvents.Count} eventos");
            return Task.FromResult(_sessionEvents.Values.ToList());
        }

        public Task UpdateAsync(SessionEvent sessionEvent)
        {
            _sessionEvents[sessionEvent.Id] = sessionEvent;
            Console.WriteLine($"[MockRepo] ATUALIZADO: {sessionEvent.Id}");
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            var sucesso = _sessionEvents.TryRemove(id, out _);
            Console.WriteLine($"[MockRepo] DELETADO: {id} → {(sucesso ? "OK" : "NÃO ENCONTRADO")}");
            return Task.CompletedTask;
        }
    }
}
