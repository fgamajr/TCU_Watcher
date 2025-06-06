using System;
using System.Collections.Generic;

namespace TCUWatcher.Domain.Entities
{
    public class SessionEvent
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public EventSourceType SourceType { get; set; }
        public string? SourceId { get; set; }
        public string? Url { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsLive { get; set; }
        public string? UploadedByUserId { get; set; }
        public List<TranscriptSegment> Transcripts { get; set; } = new();
        public List<JudgedProcess> Processes { get; set; } = new();
        public bool IsManualUpload => SourceType == EventSourceType.ManualUpload;

        // Novos campos para controle de monitoramento:
        public int MissCount { get; set; }
        public bool IsActive { get; set; }
    }
}
