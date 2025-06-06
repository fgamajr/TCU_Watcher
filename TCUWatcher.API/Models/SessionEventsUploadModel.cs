namespace TCUWatcher.API.Models
{
    public class SessionEventsUploadModel
    {
        public string Title { get; set; } = default!;
        public IFormFile VideoFile { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
    }
}
