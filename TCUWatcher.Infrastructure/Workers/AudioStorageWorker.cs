public override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    Console.WriteLine("[Worker] AudioStorageWorker iniciado.");

    while (!stoppingToken.IsCancellationRequested)
    {
        if (_audioQueue.TryDequeue(out var (path, stream)))
        {
            Console.WriteLine($"[Worker] Salvando áudio em: {path}");
            await _storageService.SaveAsync(path, stream);
        }

        await Task.Delay(100, stoppingToken);
    }
}
