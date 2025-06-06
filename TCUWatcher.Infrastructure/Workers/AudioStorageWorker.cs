using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using TCUWatcher.Domain.Services;

namespace TCUWatcher.Infrastructure.Workers
{
    // A DEFINIÇÃO DA CLASSE ESTAVA FALTANDO
    public class AudioStorageWorker : BackgroundService
    {
        private readonly IStorageService _storageService;
        // Futuramente injetar InMemoryAudioBuffer

        public AudioStorageWorker(IStorageService storageService /*, InMemoryAudioBuffer audioBuffer */)
        {
            _storageService = storageService;
            // _audioBuffer = audioBuffer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Exemplo esqueleto:
                // var item = await _audioBuffer.Reader.ReadAsync(stoppingToken);
                // await _storageService.SaveAsync(chave, new MemoryStream(item.AudioBytes));

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}