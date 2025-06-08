using System.Threading;
using System.Threading.Tasks;
using TCUWatcher.Domain.Monitoring;

namespace TCUWatcher.Domain.Services
{
    public interface IMonitoringWindowService
    {
        Task<MonitoringWindow?> GetCurrentAsync(CancellationToken ct = default);
        Task<bool> IsCurrentlyInActiveWindowAsync(CancellationToken ct = default);
    }
}
