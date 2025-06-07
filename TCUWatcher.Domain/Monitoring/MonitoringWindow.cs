using System.Text.Json.Serialization;      //  ←  precisa desse using
namespace TCUWatcher.Domain.Monitoring;

public enum MonitoringWindowStatus
{
    Open,
    Closed
}

public sealed class MonitoringWindow
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ----------- Armazenadas sempre em UTC (não serializadas) -----------
    [JsonIgnore] public DateTimeOffset StartUtc { get; init; }
    [JsonIgnore] public DateTimeOffset EndUtc   { get; init; }

    // ----------- Expostas na API / DTOs -------------------------------
    [JsonPropertyName("start")]
    public DateTimeOffset Start => StartUtc;

    [JsonPropertyName("end")]
    public DateTimeOffset End   => EndUtc;

    public MonitoringWindowStatus Status { get; private set; } = MonitoringWindowStatus.Open;

    public void Close() => Status = MonitoringWindowStatus.Closed;

    // Conveniência de domínio
    public bool Covers(DateTimeOffset instantUtc)
        => StartUtc <= instantUtc && EndUtc >= instantUtc;
}
