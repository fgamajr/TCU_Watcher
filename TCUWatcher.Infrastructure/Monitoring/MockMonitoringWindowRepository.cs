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

    public MockMonitoringWindowRepository()
{
    var now = DateTimeOffset.UtcNow;
    var baseDate = now.Date; // hoje 00:00 UTC

    foreach (var day in new[]
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    })
    {
        // Calcula o próximo dia útil correspondente
        var daysUntil = ((int)day - (int)baseDate.DayOfWeek + 7) % 7;
        var startUtc = baseDate.AddDays(daysUntil).AddHours(8);
        var endUtc = baseDate.AddDays(daysUntil).AddHours(21);

        var window = new MonitoringWindow
        {
            Id = Guid.NewGuid(),
            StartUtc = startUtc,
            EndUtc = endUtc
        };

        _store[window.Id] = window;
    }
}


}
