namespace TCUWatcher.Application.Monitoring;

using TCUWatcher.Domain.Monitoring;

public interface IMonitoringWindowRepository
{
    Task<MonitoringWindow?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(MonitoringWindow window,      CancellationToken ct = default);
    Task UpdateAsync(MonitoringWindow window,   CancellationToken ct = default);
    Task<MonitoringWindow?> FindOpenAtAsync(DateTimeOffset at, CancellationToken ct = default);

}
