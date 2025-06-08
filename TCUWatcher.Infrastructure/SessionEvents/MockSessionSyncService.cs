// Arquivo: MockSessionSyncService.cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.SessionEvents
{
    public class MockSessionSyncService : ISessionSyncService
    {
        private readonly ILogger<MockSessionSyncService> _logger;

        public MockSessionSyncService(ILogger<MockSessionSyncService> logger)
        {
            _logger = logger;
        }

        public Task SyncAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Mock] Executando sincronização de sessões...");
            // Lógica simulada de sincronização
            return Task.CompletedTask;
        }
    }
}
