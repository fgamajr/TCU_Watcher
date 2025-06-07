using System;
using System.Linq;
using System.Threading.Tasks;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Monitoring;

public class MonitoringWindowService : IMonitoringWindowService
{
    private readonly IMonitoringWindowRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MonitoringWindowService(IMonitoringWindowRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsCurrentlyInActiveWindowAsync()
    {
        var now = _timeProvider.GetUtcNow();
        var allWindows = await _repository.GetAllWindowsAsync();

        var activeWindowsForToday = allWindows
            .Where(w => w.IsEnabled && w.DayOfWeek == now.DayOfWeek);

        if (!activeWindowsForToday.Any())
        {
            return false;
        }

        var currentTime = TimeOnly.FromDateTime(now.DateTime);

        foreach (var window in activeWindowsForToday)
        {
            if (currentTime >= window.StartTimeUtc && currentTime <= window.EndTimeUtc)
            {
                return true;
            }
        }

        return false;
    }
}
