using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Video
{
    public class MockVideoDiscoveryService : IVideoDiscoveryService
    {
        private readonly ILogger<MockVideoDiscoveryService> _logger;

        public MockVideoDiscoveryService(ILogger<MockVideoDiscoveryService> logger)
        {
            _logger = logger;
        }

        public Task<IEnumerable<DiscoveredVideo>> GetLiveEventsAsync()
        {
            _logger.LogInformation("MockVideoDiscoveryService: retornando lives simuladas.");

            var fakeLive = new DiscoveredVideo
            {
                Title = "Sessão Simulada do TCU",
                VideoId = "fake123",
                Url = "https://www.youtube.com/watch?v=fake123",
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            return Task.FromResult<IEnumerable<DiscoveredVideo>>(new[] { fakeLive });
        }
    }
}
