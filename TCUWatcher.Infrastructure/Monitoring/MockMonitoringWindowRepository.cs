using System.Collections.Concurrent;
using TCUWatcher.Application.Monitoring;
using TCUWatcher.Domain.Monitoring;

namespace TCUWatcher.Infrastructure.Monitoring;

public sealed class MockMonitoringWindowRepository : IMonitoringWindowRepository
{
    private readonly ConcurrentDictionary<Guid, MonitoringWindow> _store = new();

    public Task AddAsync(MonitoringWindow window, CancellationToken ct = default)
    {
        _store[window.Id] = window;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(MonitoringWindow window, CancellationToken ct = default)
    {
        _store[window.Id] = window;
        return Task.CompletedTask;
    }

    public Task<MonitoringWindow?> GetAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var win);
        return Task.FromResult(win);
    }

    public Task<MonitoringWindow?> FindOpenAtAsync(DateTimeOffset at, CancellationToken ct = default)
    {
        var utc = at.ToUniversalTime();
        var win = _store.Values.FirstOrDefault(w =>
            w.StartUtc.ToUniversalTime() <= utc &&
            w.EndUtc  .ToUniversalTime() >= utc);

        return Task.FromResult(win);
    }
}
