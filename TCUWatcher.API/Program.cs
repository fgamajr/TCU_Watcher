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
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;

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
builder.Services.AddScoped<ISessionEventService, SessionEventService>();
// ────────────────────────────────────────────────────────────────────

// Futuramente: VideoDiscovery, HostedServices, Buffers, Workers, etc.
//
// builder.Services.AddSingleton<IVideoDiscoveryService, YouTubeVideoDiscoveryService>();
// builder.Services.AddHostedService<SessionMonitorService>();
// builder.Services.AddSingleton<IPhotographerService, FfmpegPhotographerService>();
// builder.Services.AddSingleton<IAudioCaptureService, FfmpegAudioCaptureService>();
// builder.Services.AddSingleton<InMemorySnapshotBuffer>();
// builder.Services.AddSingleton<InMemoryAudioBuffer>();
// builder.Services.AddHostedService<SnapshotStorageWorker>();
// builder.Services.AddHostedService<AudioStorageWorker>();
// ────────────────────────────────────────────────────────────────────

var app = builder.Build();

// 6) Middleware de Swagger (apenas em Dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 7) Ativar autenticação/ autorização
app.UseAuthentication();
app.UseAuthorization();

// 8) Mapear controllers
app.MapControllers();

app.Run();
