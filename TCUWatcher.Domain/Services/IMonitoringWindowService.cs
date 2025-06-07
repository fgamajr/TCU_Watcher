using System.Threading.Tasks;

namespace TCUWatcher.Domain.Services;

public interface IMonitoringWindowService
{
    Task<bool> IsCurrentlyInActiveWindowAsync();
}
