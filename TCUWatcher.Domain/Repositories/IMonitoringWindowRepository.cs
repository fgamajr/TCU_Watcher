using System.Collections.Generic;
using System.Threading.Tasks;
using TCUWatcher.Domain.Entities;

namespace TCUWatcher.Domain.Repositories;

public interface IMonitoringWindowRepository
{
    Task<IEnumerable<MonitoringWindow>> GetAllWindowsAsync();
}
