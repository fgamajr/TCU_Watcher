#!/usr/bin/env bash
set -euo pipefail

# Diretório raiz do repositório (onde está TCUWatcher.sln)
ROOT_DIR="$(pwd)"

echo "Iniciando correção e implementação dos arquivos necessários..."

#################################
# 1) Ajustar DTO de Upload em Application
#################################

echo "1) Criando CreateSessionEventWithUploadDto em TCUWatcher.Application (sem IFormFile)..."
# Em TCUWatcher.Application/SessionEvents/DTOs
mkdir -p "$ROOT_DIR/TCUWatcher.Application/SessionEvents/DTOs"
cat > "$ROOT_DIR/TCUWatcher.Application/SessionEvents/DTOs/CreateSessionEventWithUploadDto.cs" << 'EOF'
using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    /// <summary>
    /// DTO usado internamente no serviço de aplicação para tratar upload manual.
    /// Não contém IFormFile (isso fica na camada de API).
    /// </summary>
    public class CreateSessionEventWithUploadDto
    {
        public string Title { get; set; } = default!;
        public string StorageKey { get; set; } = default!;  // caminho/chave no IStorageService
        public DateTime? StartedAt { get; set; }
    }
}
EOF

#################################
# 2) Criar Interface e Service em Domain/Application como antes
#################################

echo "2) Criando IStorageService em TCUWatcher.Domain/Services..."
mkdir -p "$ROOT_DIR/TCUWatcher.Domain/Services"
cat > "$ROOT_DIR/TCUWatcher.Domain/Services/IStorageService.cs" << 'EOF'
using System.IO;
using System.Threading.Tasks;

namespace TCUWatcher.Domain.Services
{
    /// <summary>
    /// Abstração genérica para salvar, ler e remover dados (arquivos, blobs, binários)
    /// em algum storage agnóstico (disco local, Azure Blob, S3, etc.).
    /// </summary>
    public interface IStorageService
    {
        Task<string> SaveAsync(string key, Stream data);
        Task<Stream> ReadAsync(string key);
        Task DeleteAsync(string key);
    }
}
EOF

echo "2.1) Garantindo ISessionEventRepository em TCUWatcher.Domain/Repositories..."
mkdir -p "$ROOT_DIR/TCUWatcher.Domain/Repositories"
cat > "$ROOT_DIR/TCUWatcher.Domain/Repositories/ISessionEventRepository.cs" << 'EOF'
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
    }
}
EOF

#################################
# 3) MockStorageService em Infrastructure
#################################

echo "3) Criando MockStorageService em TCUWatcher.Infrastructure/Storage..."
mkdir -p "$ROOT_DIR/TCUWatcher.Infrastructure/Storage"
cat > "$ROOT_DIR/TCUWatcher.Infrastructure/Storage/MockStorageService.cs" << 'EOF'
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Storage
{
    /// <summary>
    /// Implementação em memória de IStorageService para desenvolvimento.
    /// </summary>
    public class MockStorageService : IStorageService
    {
        private static readonly ConcurrentDictionary<string, byte[]> _store 
            = new ConcurrentDictionary<string, byte[]>();

        public Task<string> SaveAsync(string key, Stream data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            using var ms = new MemoryStream();
            data.CopyTo(ms);
            _store[key] = ms.ToArray();
            return Task.FromResult(key);
        }

        public Task<Stream> ReadAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (!_store.TryGetValue(key, out var bytes))
                throw new FileNotFoundException($"Chave '{key}' não encontrada no MockStorageService.");

            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        public Task DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
EOF

#################################
# 4) MockSessionEventRepository em Infrastructure
#################################

echo "4) Criando MockSessionEventRepository em TCUWatcher.Infrastructure/SessionEvents..."
mkdir -p "$ROOT_DIR/TCUWatcher.Infrastructure/SessionEvents"
cat > "$ROOT_DIR/TCUWatcher.Infrastructure/SessionEvents/MockSessionEventRepository.cs" << 'EOF'
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
    }
}
EOF

#################################
# 5) Ajustar Service e DTOs em Application
#################################

echo "5) Atualizando ISessionEventService em TCUWatcher.Application/SessionEvents..."
mkdir -p "$ROOT_DIR/TCUWatcher.Application/SessionEvents"
cat > "$ROOT_DIR/TCUWatcher.Application/SessionEvents/ISessionEventService.cs" << 'EOF'
using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Application.SessionEvents.DTOs;

namespace TCUWatcher.Application.SessionEvents
{
    public interface ISessionEventService
    {
        Task<IEnumerable<SessionEventDto>> GetAllAsync();
        Task<SessionEventDto?> GetByIdAsync(string id);
        Task<SessionEventDto> CreateAsync(CreateSessionEventDto input);
        Task<SessionEventDto?> UpdateAsync(string id, UpdateSessionEventDto input);
        Task DeleteAsync(string id);

        // Método para upload manual
        Task<SessionEventDto> CreateWithUploadAsync(CreateSessionEventWithUploadDto input);
    }
}
EOF

echo "5.1) Criando DTOs adicionais em TCUWatcher.Application/SessionEvents/DTOs..."
mkdir -p "$ROOT_DIR/TCUWatcher.Application/SessionEvents/DTOs"

# CreateSessionEventDto
cat > "$ROOT_DIR/TCUWatcher.Application/SessionEvents/DTOs/CreateSessionEventDto.cs" << 'EOF'
using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    public class CreateSessionEventDto
    {
        public string Title { get; set; } = default!;
        public string SourceType { get; set; } = default!;
        public string? SourceId { get; set; }
        public DateTime? StartedAt { get; set; }
        public bool IsLive { get; set; }
    }
}
EOF

# UpdateSessionEventDto
cat > "$ROOT_DIR/TCUWatcher.Application/SessionEvents/DTOs/UpdateSessionEventDto.cs" << 'EOF'
using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    public class UpdateSessionEventDto
    {
        public bool IsLive { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
EOF

# SessionEventDto
cat > "$ROOT_DIR/TCUWatcher.Application/SessionEvents/DTOs/SessionEventDto.cs" << 'EOF'
using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    public class SessionEventDto
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string SourceType { get; set; } = default!;
        public string? SourceId { get; set; }
        public bool IsLive { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
EOF

echo "5.2) Implementando SessionEventService em TCUWatcher.Application/SessionEvents..."
cat > "$ROOT_DIR/TCUWatcher.Application/SessionEvents/SessionEventService.cs" << 'EOF'
using System;
using System.Collections.Generic;
using System.Linq;
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

        public SessionEventService(
            ISessionEventRepository repo,
            IStorageService storageService)
        {
            _repo = repo;
            _storageService = storageService;
        }

        public async Task<SessionEventDto> CreateAsync(CreateSessionEventDto input)
        {
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
            return all.Select(ev => new SessionEventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                SourceType = ev.SourceType.ToString(),
                SourceId = ev.SourceId,
                IsLive = ev.IsLive,
                StartedAt = ev.StartedAt,
                EndedAt = ev.EndedAt
            });
        }

        public async Task<SessionEventDto?> UpdateAsync(string id, UpdateSessionEventDto input)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.IsLive = input.IsLive;
            existing.EndedAt = input.EndedAt;
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
                    using var vidStream = await _storageService.ReadAsync(input.StorageKey);
                    vidStream.Position = 0;

                    // Exemplo simplificado: tirar apenas um snapshot no início
                    // var snapshot = await _photographerService.CaptureSnapshotFromStorageKeyAsync(
                    //     input.StorageKey, TimeSpan.Zero, CancellationToken.None);
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
                    // Logar erro
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
EOF

#################################
# 6) Ajustar Controller e DTO em API
#################################

echo "6) Movendo DTO de Upload para camada de API e ajustando Controller..."
# DTO de form / upload em API
mkdir -p "$ROOT_DIR/TCUWatcher.API/Models"
cat > "$ROOT_DIR/TCUWatcher.API/Models/SessionEventsUploadModel.cs" << 'EOF'
using System;
using Microsoft.AspNetCore.Http;

namespace TCUWatcher.API.Models
{
    /// <summary>
    /// Model para receber multipart/form-data no endpoint de upload.
    /// </summary>
    public class SessionEventsUploadModel
    {
        public string Title { get; set; } = default!;
        public IFormFile VideoFile { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
    }
}
EOF

echo "6.1) Ajustando SessionEventsController em TCUWatcher.API/Controllers..."
cat > "$ROOT_DIR/TCUWatcher.API/Controllers/SessionEventsController.cs" << 'EOF'
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCUWatcher.API.Models;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionEventsController : ControllerBase
    {
        private readonly ISessionEventService _sessionEventService;
        private readonly IStorageService _storageService;

        public SessionEventsController(
            ISessionEventService sessionEventService,
            IStorageService storageService)
        {
            _sessionEventService = sessionEventService;
            _storageService = storageService;
        }

        // GET /api/SessionEvents
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var list = await _sessionEventService.GetAllAsync();
            return Ok(list);
        }

        // GET /api/SessionEvents/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(string id)
        {
            var ev = await _sessionEventService.GetByIdAsync(id);
            if (ev == null) return NotFound();
            return Ok(ev);
        }

        // POST /api/SessionEvents (criação sem arquivo)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateSessionEventDto dto)
        {
            var created = await _sessionEventService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/SessionEvents/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSessionEventDto dto)
        {
            var updated = await _sessionEventService.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // DELETE /api/SessionEvents/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            await _sessionEventService.DeleteAsync(id);
            return NoContent();
        }

        // POST /api/SessionEvents/upload
        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(500_000_000)] // 500 MB
        public async Task<IActionResult> Upload([FromForm] SessionEventsUploadModel model)
        {
            if (model.VideoFile == null || model.VideoFile.Length == 0)
                return BadRequest("Envie um arquivo de vídeo válido.");

            var storageKey = $"manual/{Guid.NewGuid():N}_{model.VideoFile.FileName}";
            using (var stream = model.VideoFile.OpenReadStream())
            {
                await _storageService.SaveAsync(storageKey, stream);
            }

            var createDto = new CreateSessionEventWithUploadDto
            {
                Title = model.Title,
                StorageKey = storageKey,
                StartedAt = model.StartedAt ?? DateTime.UtcNow
            };

            var createdDto = await _sessionEventService.CreateWithUploadAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
        }
    }
}
EOF

#################################
# 7) Atualizar Program.cs para registrar mocks
#################################

echo "7) Atualizando Program.cs em TCUWatcher.API..."
cat > "$ROOT_DIR/TCUWatcher.API/Program.cs" << 'EOF'
using TCUWatcher.Application.Users;
using TCUWatcher.API.Authentication;
using TCUWatcher.Infrastructure.Users;
using TCUWatcher.API.Extensions;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Infrastructure.SessionEvents;
using TCUWatcher.Domain.Services;
using TCUWatcher.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Serviços fundamentais
builder.Services.AddControllers();

// 2) Registrar mocks de usuário
builder.Services.AddSingleton<ICurrentUserProvider, MockUserProvider>();
builder.Services.AddScoped<IUserService, MockUserService>();
builder.Services.AddScoped<IAuthenticationService, MockAuthenticationService>();

// 3) Registrar esquema “Bearer mock”
builder.Services.AddMockAuthentication();

// 4) Configurar Swagger/OpenAPI com Bearer
builder.Services.AddSwaggerWithBearer();

// ────────────────────────────────────────────────────────────────────
// 5) Registrar repositório de sessão e storage mock
builder.Services.AddScoped<ISessionEventRepository, MockSessionEventRepository>();
builder.Services.AddSingleton<IStorageService, MockStorageService>();
// ────────────────────────────────────────────────────────────────────

// Futuramente: VideoDiscovery, HostedServices, Buffers, Workers, etc.
// builder.Services.AddSingleton<IVideoDiscoveryService, YouTubeVideoDiscoveryService>();
// builder.Services.AddHostedService<SessionMonitorService>();
// builder.Services.AddSingleton<IPhotographerService, FfmpegPhotographerService>();
// builder.Services.AddSingleton<IAudioCaptureService, FfmpegAudioCaptureService>();
// builder.Services.AddSingleton<InMemorySnapshotBuffer>();
// builder.Services.AddSingleton<InMemoryAudioBuffer>();
// builder.Services.AddHostedService<SnapshotStorageWorker>();
// builder.Services.AddHostedService<AudioStorageWorker>();

var app = builder.Build();

// 6) Middleware de Swagger (apenas em Dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
EOF

#################################
# 8) Ajustar Entity SessionEvent em Domain
#################################

echo "8) Ajustando entidade SessionEvent em TCUWatcher.Domain/Entities..."
ENTITY_FILE="$ROOT_DIR/TCUWatcher.Domain/Entities/SessionEvent.cs"
cat > "$ENTITY_FILE" << 'EOF'
using System;
using System.Collections.Generic;

namespace TCUWatcher.Domain.Entities
{
    public class SessionEvent
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public EventSourceType SourceType { get; set; }
        public string? SourceId { get; set; }
        public string? Url { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsLive { get; set; }
        public string? UploadedByUserId { get; set; }
        public List<TranscriptSegment> Transcripts { get; set; } = new();
        public List<JudgedProcess> Processes { get; set; } = new();
        public bool IsManualUpload => SourceType == EventSourceType.ManualUpload;

        // Novo campos para controle:
        public int MissCount { get; set; }
        public bool IsActive { get; set; }
    }
}
EOF

#################################
# 9) Skeletons de Workers em Infrastructure
#################################

echo "9) Criando esqueleto de workers em TCUWatcher.Infrastructure/Workers..."
mkdir -p "$ROOT_DIR/TCUWatcher.Infrastructure/Workers"

cat > "$ROOT_DIR/TCUWatcher.Infrastructure/Workers/SnapshotStorageWorker.cs" << 'EOF'
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Workers
{
    public class SnapshotStorageWorker : BackgroundService
    {
        private readonly IStorageService _storageService;
        // Futuramente injetar InMemorySnapshotBuffer

        public SnapshotStorageWorker(IStorageService storageService /*, InMemorySnapshotBuffer snapshotBuffer */)
        {
            _storageService = storageService;
            // _snapshotBuffer = snapshotBuffer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Exemplo esqueleto:
                // var item = await _snapshotBuffer.Reader.ReadAsync(stoppingToken);
                // await _storageService.SaveAsync(chave, new MemoryStream(item.ImageBytes));

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
EOF

cat > "$ROOT_DIR/TCUWatcher.Infrastructure/Workers/AudioStorageWorker.cs" << 'EOF'
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Workers
{
    public class AudioStorageWorker : BackgroundService
    {
        private readonly IStorageService _storageService;
        // Futuramente injetar InMemoryAudioBuffer

        public AudioStorageWorker(IStorageService storageService /*, InMemoryAudioBuffer audioBuffer */)
        {
            _storageService = storageService;
            // _audioBuffer = audioBuffer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Exemplo esqueleto:
                // var item = await _audioBuffer.Reader.ReadAsync(stoppingToken);
                // await _storageService.SaveAsync(chave, new MemoryStream(item.AudioBytes));

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
EOF

#################################
# 10) Mensagem Final
#################################

echo
echo "==========================================="
echo "Todos os arquivos base foram criados/ajustados!"
echo
echo "Próximos passos:"
echo " 1) Suba a API: dotnet run --project TCUWatcher.API"
echo " 2) No Swagger (/swagger), faça login e teste:"
echo "    POST /api/SessionEvents/upload"
echo "    - Title: 'Teste Upload Manual'"
echo "    - VideoFile: selecione um .mp4"
echo "    - StartedAt: (opcional)"
echo "    - Deve retornar 201 Created com JSON do SessionEvent."
echo " 3) Verifique se MockStorageService armazenou em memória. (Não há UI; você pode depurar ou adicionar logging.)"
echo
echo "Depois, implemente:"
echo "  - InMemorySnapshotBuffer e InMemoryAudioBuffer"
echo "  - IPhotographerService e IAudioCaptureService"
echo "  - Registre SnapshotStorageWorker e AudioStorageWorker em Program.cs"
echo "  - Desenvolva lógica para empurrar snapshots/áudio para buffers e salvá-los via IStorageService."
echo
echo "==========================================="
