using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Services; // <--- O 'using' mais importante para este erro



namespace TCUWatcher.Infrastructure.BackgroundServices;

public class VideoDiscoveryWorker : BackgroundService
{
    private readonly ILogger<VideoDiscoveryWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public VideoDiscoveryWorker(ILogger<VideoDiscoveryWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VideoDiscoveryWorker iniciado.");
        while (!stoppingToken.IsCancellationRequested)
        {
            // Criamos um escopo para cada "ronda" do worker para usar serviços Scoped
            using (var scope = _serviceProvider.CreateScope())
            {
                var monitoringService = scope.ServiceProvider.GetRequiredService<IMonitoringWindowService>();
                var videoDiscoveryService = scope.ServiceProvider.GetRequiredService<IVideoDiscoveryService>();
                var sessionEventService = scope.ServiceProvider.GetRequiredService<ISessionEventService>();

                if (await monitoringService.IsCurrentlyInActiveWindowAsync())
                {
                    _logger.LogWarning(">>> DENTRO DA JANELA DE MONITORAMENTO. Buscando vídeos... <<<");
                    
                    var liveVideos = await videoDiscoveryService.GetLiveEventsAsync();
                    if (liveVideos.Any())
                    {
                         foreach (var video in liveVideos)
                        {
                            _logger.LogInformation("  -> VÍDEO ENCONTRADO: '{Title}'. Criando SessionEvent.", video.Title);
                            await sessionEventService.CreateAsync(new CreateSessionEventDto
                            {
                                Title = video.Title,
                                SourceId = video.VideoId,
                                SourceType = "YouTube",
                                IsLive = true,
                                StartedAt = video.StartedAt
                            });
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Nenhum vídeo ao vivo encontrado nesta verificação.");
                    }
                }
                else
                {
                    _logger.LogInformation("Fora da janela de monitoramento. Descansando...");
                }
            }
            
            // Em uma simulação rápida, este delay pode ser menor.
            // Em produção, seria algo como 5 minutos.
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
        }
    }
}