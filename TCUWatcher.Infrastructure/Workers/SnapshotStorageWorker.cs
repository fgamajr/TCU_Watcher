using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Workers
{
    // A DEFINIÇÃO DA CLASSE ESTAVA FALTANDO
    public class SnapshotStorageWorker : BackgroundService
    {
        private readonly IStorageService _storageService;
        // Futuramente injetar InMemorySnapshotBuffer

        public SnapshotStorageWorker(IStorageService storageService /*, InMemorySnapshotBuffer snapshotBuffer */)
        {
            _storageService = storageService;
            // _snapshotBuffer = snapshotBuffer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Exemplo esqueleto:
                // var item = await _snapshotBuffer.Reader.ReadAsync(stoppingToken);
                // await _storageService.SaveAsync(chave, new MemoryStream(item.ImageBytes));

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}