namespace TCUWatcher.Application.LiveEvents.DTOs;

public class CreateLiveEventDto
{
    public string Title { get; set; } = default!;
    public string SourceType { get; set; } = default!; // ex: \"YouTube\" ou \"ManualUpload\"
    public string? SourceId { get; set; }
    public DateTime? StartedAt { get; set; }
    public bool IsLive { get; set; }
}
