using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Common;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Errors;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Application.SessionEvents
{
    public class SessionEventService : ISessionEventService
    {
        private readonly ISessionEventRepository _repo;
        private readonly IStorageService _storageService;
        private readonly ILogger<SessionEventService> _logger;
        private readonly IMapper _mapper;

        public SessionEventService(ISessionEventRepository repo, IStorageService storageService, ILogger<SessionEventService> logger, IMapper mapper)
        {
            _repo = repo;
            _storageService = storageService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<SessionEventDto, DomainError>> CreateAsync(CreateSessionEventDto input)
        {
            var sessionEvent = _mapper.Map<SessionEvent>(input);
            sessionEvent.Id = Guid.NewGuid().ToString();
            sessionEvent.IsActive = input.IsLive;
            await _repo.AddAsync(sessionEvent);
            var resultDto = _mapper.Map<SessionEventDto>(sessionEvent);
            return Result<SessionEventDto, DomainError>.Success(resultDto);
        }

        public async Task<Result<SessionEventDto, DomainError>> GetByIdAsync(string id)
        {
            var ev = await _repo.GetByIdAsync(id);
            if (ev == null)
            {
                return Result<SessionEventDto, DomainError>.Failure(new NotFoundError(nameof(SessionEvent), id));
            }
            return Result<SessionEventDto, DomainError>.Success(_mapper.Map<SessionEventDto>(ev));
        }

        public async Task<IEnumerable<SessionEventDto>> GetAllAsync()
        {
            var all = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<SessionEventDto>>(all);
        }

        public async Task<Result<SessionEventDto, DomainError>> UpdateAsync(string id, UpdateSessionEventDto input)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return Result<SessionEventDto, DomainError>.Failure(new NotFoundError(nameof(SessionEvent), id));
            }

            existing.IsLive = input.IsLive;
            existing.EndedAt = input.EndedAt;
            await _repo.UpdateAsync(existing);
            return Result<SessionEventDto, DomainError>.Success(_mapper.Map<SessionEventDto>(existing));
        }

        public async Task<Result<bool, DomainError>> DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            return Result<bool, DomainError>.Success(true);
        }

        public async Task<Result<SessionEventDto, DomainError>> CreateWithUploadAsync(CreateSessionEventWithUploadDto input)
        {
            var nowUtc = DateTime.UtcNow;
            var sessionEvent = new SessionEvent
            {
                Id = Guid.NewGuid().ToString(),
                Title = input.Title,
                SourceType = EventSourceType.ManualUpload,
                SourceId = input.StorageKey,
                StartedAt = input.StartedAt ?? nowUtc,
                EndedAt = input.StartedAt ?? nowUtc,
                IsLive = false,
                IsActive = false
            };
            await _repo.AddAsync(sessionEvent);
            _logger.LogInformation("Sessão de Upload Manual criada com ID: {SessionId}. Iniciando processamento em background.", sessionEvent.Id);

            return Result<SessionEventDto, DomainError>.Success(_mapper.Map<SessionEventDto>(sessionEvent));
        }
    }
}
