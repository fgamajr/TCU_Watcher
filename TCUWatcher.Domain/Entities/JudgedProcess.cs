namespace TCUWatcher.Domain.Entities;

public class JudgedProcess
{
    public string Id { get; set; } = default!;
    public string LiveEventId { get; set; } = default!;
    public string CaseNumber { get; set; } = default!;
    public string? Title { get; set; }
    public string? Status { get; set; }
    public List<TranscriptSegment> TranscriptSegments { get; set; } = new();
}
