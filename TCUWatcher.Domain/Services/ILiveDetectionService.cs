using System.Threading;
using System.Threading.Tasks;

namespace TCUWatcher.Domain.Services
{
    public interface ILiveDetectionService
    {
        Task DetectLiveSessionsAsync(CancellationToken cancellationToken = default);
    }
}
