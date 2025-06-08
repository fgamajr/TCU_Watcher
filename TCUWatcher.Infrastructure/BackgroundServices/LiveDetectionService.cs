using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.BackgroundServices
{
    public class LiveDetectionService : ILiveDetectionService
    {
        private readonly ISessionEventRepository _sessionRepo;
        private readonly ILogger<LiveDetectionService> _logger;

        public LiveDetectionService(ISessionEventRepository sessionRepo, ILogger<LiveDetectionService> logger)
        {
            _sessionRepo = sessionRepo;
            _logger = logger;
        }

        public async Task DetectLiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-2);
            var windowEnd = now.AddMinutes(2);

            var liveSessions = await _sessionRepo.GetSessionsInTimeRangeAsync(windowStart, windowEnd);
            var liveIds = liveSessions.Select(s => s.Id).ToHashSet();

            var allSessions = await _sessionRepo.GetAllAsync();

            foreach (var session in allSessions)
            {
                bool shouldBeLive = liveIds.Contains(session.Id);
                if (shouldBeLive && !session.IsLive)
                {
                    session.IsLive = true;
                    await _sessionRepo.UpdateAsync(session);
                    _logger.LogInformation("Sessão '{Title}' marcada como AO VIVO.", session.Title);
                }
                else if (!shouldBeLive && session.IsLive)
                {
                    session.IsLive = false;
                    await _sessionRepo.UpdateAsync(session);
                    _logger.LogInformation("Sessão '{Title}' marcada como NÃO AO VIVO.", session.Title);
                }
            }
        }
    }

}
