using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper; // <-- 1. IMPORTAR O AUTOMAPPER
using Microsoft.Extensions.Logging;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Application.SessionEvents
{
    public class SessionEventService : ISessionEventService
    {
        private readonly ISessionEventRepository _repo;
        private readonly IStorageService _storageService;
        private readonly ILogger<SessionEventService> _logger;
        private readonly IMapper _mapper; // <-- 2. CAMPO PARA O MAPPER

        public SessionEventService(
            ISessionEventRepository repo,
            IStorageService storageService,
            ILogger<SessionEventService> logger,
            IMapper mapper) // <-- 3. INJETAR O IMAPPER
        {
            _repo = repo;
            _storageService = storageService;
            _logger = logger;
            _mapper = mapper; // <-- 4. ARMAZENAR
        }

        public async Task<SessionEventDto> CreateAsync(CreateSessionEventDto input)
        {
            var sessionEvent = _mapper.Map<SessionEvent>(input); // <-- MÁGICA!
            sessionEvent.Id = Guid.NewGuid().ToString();
            // Outras lógicas que o AutoMapper não faz por padrão
            sessionEvent.IsActive = input.IsLive; 
            
            await _repo.AddAsync(sessionEvent);

            return _mapper.Map<SessionEventDto>(sessionEvent); // <-- MÁGICA DE NOVO!
        }

        public async Task<SessionEventDto?> GetByIdAsync(string id)
        {
            var ev = await _repo.GetByIdAsync(id);
            return _mapper.Map<SessionEventDto>(ev); // <-- E LÁ VAMOS NÓS!
        }

        public async Task<IEnumerable<SessionEventDto>> GetAllAsync()
        {
            var all = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<SessionEventDto>>(all); // <-- ATÉ PARA LISTAS!
        }

        public async Task<SessionEventDto?> UpdateAsync(string id, UpdateSessionEventDto input)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.IsLive = input.IsLive;
            existing.EndedAt = input.EndedAt;
            await _repo.UpdateAsync(existing);

            return _mapper.Map<SessionEventDto>(existing);
        }

        public async Task DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
        }

        // ... O método CreateWithUploadAsync continua igual, pois ele já tem uma lógica mais complexa.
        // Mas a linha de retorno dele também pode usar o AutoMapper:
        public async Task<SessionEventDto> CreateWithUploadAsync(CreateSessionEventWithUploadDto input)
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
            _ = Task.Run(async () =>
            {
                try
                {
                    using var vidStream = await _storageService.ReadAsync(input.StorageKey);
                    _logger.LogInformation("Processamento em background da sessão {SessionId} concluído com sucesso.", sessionEvent.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha crítica ao processar o vídeo em background. StorageKey: {StorageKey}, SessionId: {SessionId}", input.StorageKey, sessionEvent.Id);
                }
            });

            return _mapper.Map<SessionEventDto>(sessionEvent); // <-- ÚLTIMA LIMPEZA!
        }
    }
}