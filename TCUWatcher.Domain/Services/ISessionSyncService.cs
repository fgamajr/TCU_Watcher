using System.Threading;
using System.Threading.Tasks;

namespace TCUWatcher.Domain.Services
{
    public interface ISessionSyncService
    {
        Task SyncAsync(CancellationToken cancellationToken = default);
    }
}
