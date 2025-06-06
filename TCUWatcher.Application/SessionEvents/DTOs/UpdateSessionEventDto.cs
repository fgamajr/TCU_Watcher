using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    public class UpdateSessionEventDto
    {
        public bool IsLive { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
