using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Repositories;

namespace TCUWatcher.Infrastructure.Monitoring;

public class MockMonitoringWindowRepository : IMonitoringWindowRepository
{
    private readonly List<MonitoringWindow> _windows = new()
    {
        new MonitoringWindow { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Tuesday, StartTimeUtc = new TimeOnly(14, 0), EndTimeUtc = new TimeOnly(18, 0), IsEnabled = true },
        new MonitoringWindow { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Thursday, StartTimeUtc = new TimeOnly(10, 0), EndTimeUtc = new TimeOnly(12, 0), IsEnabled = true },
        new MonitoringWindow { Id = Guid.NewGuid(), DayOfWeek = DayOfWeek.Friday, StartTimeUtc = new TimeOnly(9, 0), EndTimeUtc = new TimeOnly(11, 0), IsEnabled = false }
    };

    public Task<IEnumerable<MonitoringWindow>> GetAllWindowsAsync()
    {
        return Task.FromResult(_windows.AsEnumerable());
    }
}
