using System;
using System.IO;
using System.Threading.Tasks;
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
        // Futuramente poderão ser injetados _photographerService, _audioCaptureService, buffers, etc.

        public SessionEventService(
            ISessionEventRepository repo,
            IStorageService storageService)
        {
            _repo = repo;
            _storageService = storageService;
        }

        public async Task<SessionEventDto> CreateAsync(CreateSessionEventDto input)
        {
            // Implementação existente ou stub
            var sessionEvent = new SessionEvent
            {
                Id = Guid.NewGuid().ToString(),
                Title = input.Title,
                SourceType = Enum.Parse<EventSourceType>(input.SourceType),
                SourceId = input.SourceId,
                Url = null,
                StartedAt = input.StartedAt,
                EndedAt = null,
                IsLive = input.IsLive,
                MissCount = 0,
                IsActive = input.IsLive
            };

            await _repo.AddAsync(sessionEvent);

            return new SessionEventDto
            {
                Id = sessionEvent.Id,
                Title = sessionEvent.Title,
                SourceType = sessionEvent.SourceType.ToString(),
                SourceId = sessionEvent.SourceId,
                IsLive = sessionEvent.IsLive,
                StartedAt = sessionEvent.StartedAt,
                EndedAt = sessionEvent.EndedAt
            };
        }

        public async Task DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<SessionEventDto?> GetByIdAsync(string id)
        {
            var ev = await _repo.GetByIdAsync(id);
            if (ev == null) return null;
            return new SessionEventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                SourceType = ev.SourceType.ToString(),
                SourceId = ev.SourceId,
                IsLive = ev.IsLive,
                StartedAt = ev.StartedAt,
                EndedAt = ev.EndedAt
            };
        }

        public async Task<IEnumerable<SessionEventDto>> GetAllAsync()
        {
            var all = await _repo.GetAllAsync();
            var result = all.Select(ev => new SessionEventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                SourceType = ev.SourceType.ToString(),
                SourceId = ev.SourceId,
                IsLive = ev.IsLive,
                StartedAt = ev.StartedAt,
                EndedAt = ev.EndedAt
            });
            return result;
        }

        public async Task<SessionEventDto?> UpdateAsync(string id, UpdateSessionEventDto input)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.IsLive = input.IsLive;
            existing.EndedAt = input.EndedAt;
            // Ajustar outros campos se necessário
            await _repo.UpdateAsync(existing);

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

        public async Task<SessionEventDto> CreateWithUploadAsync(CreateSessionEventWithUploadDto input)
        {
            var nowUtc = DateTime.UtcNow;
            var sessionEvent = new SessionEvent
            {
                Id = Guid.NewGuid().ToString(),
                Title = input.Title,
                SourceType = EventSourceType.ManualUpload,
                SourceId = input.StorageKey,
                Url = null,
                StartedAt = input.StartedAt ?? nowUtc,
                EndedAt = input.StartedAt ?? nowUtc,
                IsLive = false,
                MissCount = 2,
                IsActive = false
            };

            await _repo.AddAsync(sessionEvent);

            // Disparar processamento assíncrono (upload já está no Storage)
            _ = Task.Run(async () =>
            {
                try
                {
                    // Ler do storage
                    using var vidStream = await _storageService.ReadAsync(input.StorageKey);

                    // Exemplo simplificado: tirar apenas UM snapshot no início
                    vidStream.Position = 0;
                    // Chamada fictícia, implementar _photographerService depois
                    // var snapshot = await _photographerService.CaptureSnapshotFromStorageKeyAsync(
                    //     input.StorageKey, TimeSpan.Zero, CancellationToken.None);
                    //
                    // _snapshotBuffer.Writer.TryWrite(new SnapshotItem
                    // {
                    //     SessionEventId = sessionEvent.Id,
                    //     ImageBytes = snapshot.ToArray(),
                    //     CapturedAt = DateTime.UtcNow
                    // });

                    // Extrair todo o áudio em memória
                    // var recId = await _audioCaptureService.StartRecordingFromStorageKeyAsync(input.StorageKey, CancellationToken.None);
                    // var audioStream = await _audioCaptureService.StopRecordingAsync(recId, CancellationToken.None);
                    // _audioBuffer.Writer.TryWrite(new AudioChunk
                    // {
                    //     SessionEventId = sessionEvent.Id,
                    //     AudioBytes = audioStream.ToArray(),
                    //     SegmentStart = TimeSpan.Zero
                    // });
                }
                catch
                {
                    // Log de erro, se quiser
                }
            });

            return new SessionEventDto
            {
                Id = sessionEvent.Id,
                Title = sessionEvent.Title,
                SourceType = sessionEvent.SourceType.ToString(),
                SourceId = sessionEvent.SourceId,
                IsLive = sessionEvent.IsLive,
                StartedAt = sessionEvent.StartedAt,
                EndedAt = sessionEvent.EndedAt
            };
        }
    }
}
