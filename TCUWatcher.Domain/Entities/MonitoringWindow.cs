using System;

namespace TCUWatcher.Domain.Entities;

public class MonitoringWindow
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTimeUtc { get; set; }
    public TimeOnly EndTimeUtc { get; set; }
    public bool IsEnabled { get; set; }
}
