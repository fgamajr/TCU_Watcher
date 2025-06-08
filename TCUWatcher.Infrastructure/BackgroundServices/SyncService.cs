using TCUWatcher.Application.Monitoring;
using TCUWatcher.Infrastructure.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TCUWatcher.Domain.Services;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;

namespace TCUWatcher.Infrastructure.BackgroundServices
{
    public class SyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncService> _logger;

        public SyncService(IServiceScopeFactory scopeFactory, ILogger<SyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var monitoringService = scope.ServiceProvider.GetRequiredService<IMonitoringWindowService>();
                    var videoDiscovery = scope.ServiceProvider.GetRequiredService<IVideoDiscoveryService>();
                    var sessionEventService = scope.ServiceProvider.GetRequiredService<ISessionEventService>();

                    var dentroDaJanela = await monitoringService.IsCurrentlyInActiveWindowAsync(stoppingToken);

                    if (dentroDaJanela)
                    {
                        var lives = await videoDiscovery.GetLiveEventsAsync();

                        foreach (var live in lives)
                        {
                            _logger.LogInformation("Live detectada: {Title} ({VideoId})", live.Title, live.VideoId);

                            // var createDto = new CreateSessionEventDto
                            // {
                            //     Title = live.Title,
                            //     VideoId = live.VideoId,
                            //     Url = live.Url,
                            //     StartedAt = live.StartedAt,
                            //     Source = Domain.Entities.EventSourceType.Live
                            // };

                            var createDto = new CreateSessionEventDto
                            {
                                Title = live.Title,
                                SourceId = live.VideoId,
                                Url = live.Url, // Remover esta linha (não existe no DTO)
                                SourceType = "YouTube",
                                StartedAt = live.StartedAt,
                                IsLive = true
                            };


                            var result = await sessionEventService.CreateAsync(createDto);

                            if (result.IsSuccess)
                            {
                                _logger.LogInformation("Evento de sessão criado com sucesso para a live {VideoId}", live.VideoId);
                            }
                            else
                            {
                                _logger.LogWarning("Falha ao criar evento de sessão para {VideoId}: {Error}",
                                    live.VideoId, result.Error?.Message ?? "Erro desconhecido");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Fora da janela de monitoramento. Nenhuma live será buscada.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante execução do SyncService");
                }

                // Esperar um pouco antes da próxima execução (ex: 1 minuto)
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }

            _logger.LogInformation("SyncService finalizado.");
        }
    }
}
