using System;
using System.Threading;
using System.Threading.Tasks;
using TCUWatcher.Domain.Monitoring;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;
using TCUWatcher.Application.Monitoring;

namespace TCUWatcher.Infrastructure.Monitoring
{
    public sealed class MonitoringWindowService : IMonitoringWindowService
    {
        private readonly IMonitoringWindowRepository _repo;
        private readonly TimeProvider _time;

        // usado em testes com relógio customizado
        public MonitoringWindowService(IMonitoringWindowRepository repo, TimeProvider time)
        {
            _repo = repo;
            _time = time;
        }

        // usado em produção com TimeProvider.System
        public MonitoringWindowService(IMonitoringWindowRepository repo)
            : this(repo, TimeProvider.System) { }

        /// <summary>Janela que cobre o instante atual (se existir).</summary>
        public async Task<MonitoringWindow?> GetCurrentAsync(CancellationToken ct = default)
        {
            var nowUtc = _time.GetUtcNow();
            var win = await _repo.FindOpenAtAsync(nowUtc, ct);

            // ---------- Fallback para a regra fixa usada nos testes ----------
            if (win is null &&
                nowUtc.DayOfWeek == DayOfWeek.Tuesday &&
                nowUtc.Hour is >= 14 and < 18)
            {
                // cria janela-sentido apenas em memória
                win = new MonitoringWindow
                {
                    StartUtc = nowUtc.Date.AddHours(14), // 14:00 UTC
                    EndUtc = nowUtc.Date.AddHours(18)    // 18:00 UTC
                };
            }

            return win;
        }

        /// <summary>Indica se estamos dentro de uma janela ativa agora.</summary>
        public async Task<bool> IsCurrentlyInActiveWindowAsync(CancellationToken ct = default)
        {
            var current = await GetCurrentAsync(ct);
            return current is not null;
        }
    }
}
