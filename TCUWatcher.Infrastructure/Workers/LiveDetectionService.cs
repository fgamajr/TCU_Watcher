using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;



namespace TCUWatcher.Infrastructure.Workers
{
    public class LiveDetectionService : BackgroundService
    {
        private readonly ILogger<LiveDetectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval;

        public LiveDetectionService(
            ILogger<LiveDetectionService> logger,
            IServiceProvider serviceProvider,
            IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            var intervalSeconds = config.GetValue<int>("LiveDetection:IntervalSeconds", 1800);
            _interval = TimeSpan.FromSeconds(intervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LiveDetectionService iniciado. Verificando sessões a cada {IntervalMinutes} minutos.", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var monitoringWindowService = scope.ServiceProvider.GetRequiredService<IMonitoringWindowService>();
                    var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionEventRepository>();

                    var isInsideWindow = await monitoringWindowService.IsCurrentlyInActiveWindowAsync();

                    var now = DateTime.UtcNow;
                    var windowStart = now.AddMinutes(-2);
                    var windowEnd = now.AddMinutes(2);

                    if (!isInsideWindow)
                    {
                        _logger.LogInformation("Fora da janela de monitoramento. Ignorando execução. (UTC agora: {Now})", now);
                    }
                    else
                    {
                        _logger.LogInformation("Dentro da janela de monitoramento (UTC agora: {Now}). Verificando sessões com início entre {Start} e {End}.", now, windowStart, windowEnd);

                        var liveSessions = await sessionRepo.GetSessionsInTimeRangeAsync(windowStart, windowEnd);
                        var liveIds = liveSessions.Select(s => s.Id).ToHashSet();

                        _logger.LogInformation("{Count} sessões encontradas dentro da janela de monitoramento.", liveIds.Count);

                        var allSessions = await sessionRepo.GetAllAsync();

                        foreach (var session in allSessions)
                        {
                            bool shouldBeLive = liveIds.Contains(session.Id);
                            if (shouldBeLive && !session.IsLive)
                            {
                                session.IsLive = true;
                                await sessionRepo.UpdateAsync(session);
                                _logger.LogInformation("Sessão '{Title}' marcada como AO VIVO.", session.Title);
                            }
                            else if (!shouldBeLive && session.IsLive)
                            {
                                session.IsLive = false;
                                await sessionRepo.UpdateAsync(session);
                                _logger.LogInformation("Sessão '{Title}' marcada como NÃO AO VIVO.", session.Title);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar LiveDetectionService.");
                }

                _logger.LogInformation("A próxima verificação ocorrerá em {Interval} minutos...",  _interval.TotalMinutes);
                await Task.Delay(_interval, stoppingToken);
            }
        }

    }
}
