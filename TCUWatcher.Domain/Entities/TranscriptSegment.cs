namespace TCUWatcher.Domain.Entities;

public class TranscriptSegment
{
    public string Id { get; set; } = default!;
    public string LiveEventId { get; set; } = default!;
    public string? JudgedProcessId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = default!;
    public string? Speaker { get; set; }
}
