namespace TCUWatcher.Domain.Services;

public class DiscoveredVideo
{
    public string Title { get; set; } = default!;
    public string VideoId { get; set; } = default!;
    public string Url { get; set; } = default!;
    public DateTime StartedAt { get; set; }
}

public interface IVideoDiscoveryService
{
    Task<IEnumerable<DiscoveredVideo>> GetLiveEventsAsync();
}
