using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    public class CreateSessionEventDto
    {
        public string Title { get; set; } = default!;
        public string SourceType { get; set; } = default!; // ex: "YouTube" ou "ManualUpload"
        public string? SourceId { get; set; }
        public DateTime? StartedAt { get; set; }
        public bool IsLive { get; set; }
        public string? Url { get; set; } // URL opcional, pode ser nulo se não for uma live
    }
}
