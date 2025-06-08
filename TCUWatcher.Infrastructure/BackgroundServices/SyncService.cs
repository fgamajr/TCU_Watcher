using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.BackgroundServices
{
    public class SyncService : BackgroundService
    {
        private readonly ILogger<SyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval;

        public SyncService(
            ILogger<SyncService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

            var intervalSeconds = configuration.GetValue<int>("LiveDetection:IntervalSeconds", 1800);
            _interval = TimeSpan.FromSeconds(intervalSeconds);
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
                    var liveDetectionService = scope.ServiceProvider.GetRequiredService<ILiveDetectionService>();
                    var sessionSyncService = scope.ServiceProvider.GetRequiredService<ISessionSyncService>();

                    var dentroDaJanela = await monitoringService.IsCurrentlyInActiveWindowAsync(stoppingToken);

                    if (dentroDaJanela)
                    {
                        _logger.LogInformation("Dentro da janela de monitoramento. Iniciando verificação de lives...");
                        await liveDetectionService.DetectLiveSessionsAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("Fora da janela de monitoramento. Não serão buscadas lives.");
                    }

                    // Sincronizar o estado do banco de dados (live ou manual upload)
                    await sessionSyncService.SyncAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante execução do SyncService.");
                }

                _logger.LogInformation("A próxima verificação ocorrerá em {Minutes} minutos...", _interval.TotalMinutes);
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("SyncService finalizado.");
        }
    }
}
