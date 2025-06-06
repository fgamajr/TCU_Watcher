using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    public class SessionEventDto
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string SourceType { get; set; } = default!;
        public string? SourceId { get; set; }
        public bool IsLive { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
