public override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    Console.WriteLine("[Worker] SnapshotStorageWorker iniciado.");

    while (!stoppingToken.IsCancellationRequested)
    {
        if (_snapshotQueue.TryDequeue(out var (path, stream)))
        {
            Console.WriteLine($"[Worker] Salvando snapshot em: {path}");
            await _storageService.SaveAsync(path, stream);
        }

        await Task.Delay(100, stoppingToken);
    }
}
