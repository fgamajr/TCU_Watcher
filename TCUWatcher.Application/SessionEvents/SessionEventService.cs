// TCUWatcher.Application/SessionEvents/SessionEventService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Repositories;

namespace TCUWatcher.Application.SessionEvents
{
    public class SessionEventService : ISessionEventService
    {
        private readonly ISessionEventRepository _repository;

        public SessionEventService(ISessionEventRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SessionEventDto>> GetAllAsync()
        {
            var events = await _repository.GetAllAsync();
            return events.Select(e => new SessionEventDto
            {
                Id = e.Id,
                Title = e.Title,
                SourceType = e.SourceType.ToString(),
                SourceId = e.SourceId,
                IsLive = e.IsLive,
                StartedAt = e.StartedAt,
                EndedAt = e.EndedAt
            });
        }

        public async Task<SessionEventDto?> GetByIdAsync(string id)
        {
            var e = await _repository.GetByIdAsync(id);
            if (e == null) return null;
            return new SessionEventDto
            {
                Id = e.Id,
                Title = e.Title,
                SourceType = e.SourceType.ToString(),
                SourceId = e.SourceId,
                IsLive = e.IsLive,
                StartedAt = e.StartedAt,
                EndedAt = e.EndedAt
            };
        }

        public async Task<SessionEventDto> CreateAsync(CreateSessionEventDto input)
        {
            var newEvent = new SessionEvent
            {
                Id = Guid.NewGuid().ToString(),
                Title = input.Title,
                SourceType = Enum.TryParse<EventSourceType>(input.SourceType, true, out var st)
                             ? st : EventSourceType.Other,
                SourceId = input.SourceId,
                StartedAt = input.StartedAt,
                IsLive = input.IsLive,
                EndedAt = null
            };
            await _repository.AddAsync(newEvent);

            return new SessionEventDto
            {
                Id = newEvent.Id,
                Title = newEvent.Title,
                SourceType = newEvent.SourceType.ToString(),
                SourceId = newEvent.SourceId,
                IsLive = newEvent.IsLive,
                StartedAt = newEvent.StartedAt,
                EndedAt = newEvent.EndedAt
            };
        }

        public async Task<SessionEventDto?> UpdateAsync(string id, UpdateSessionEventDto input)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return null;

            if (input.IsLive.HasValue)
                existing.IsLive = input.IsLive.Value;

            if (input.EndedAt.HasValue)
                existing.EndedAt = input.EndedAt.Value;

            await _repository.UpdateAsync(existing);

            return new SessionEventDto
            {
                Id = existing.Id,
                Title = existing.Title,
                SourceType = existing.SourceType.ToString(),
                SourceId = existing.SourceId,
                IsLive = existing.IsLive,
                StartedAt = existing.StartedAt,
                EndedAt = existing.EndedAt
            };
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return false;

            await _repository.DeleteAsync(id);
            return true;
        }
    }
}
