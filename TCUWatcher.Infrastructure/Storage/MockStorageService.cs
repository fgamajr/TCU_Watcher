using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using TCUWatcher.Domain.Storage;

namespace TCUWatcher.Infrastructure.Storage
{
    public class MockStorageService : IStorageService
    {
        private readonly ConcurrentDictionary<string, MemoryStream> _storage = new();

        public Task SaveAsync(string path, Stream content)
        {
            var memoryStream = new MemoryStream();
            content.CopyTo(memoryStream);
            _storage[path] = memoryStream;

            Console.WriteLine($"[MockStorage] SALVO: {path} ({memoryStream.Length} bytes)");
            return Task.CompletedTask;
        }

        public Task<Stream> ReadAsync(string path)
        {
            if (_storage.TryGetValue(path, out var stream))
            {
                stream.Position = 0;
                Console.WriteLine($"[MockStorage] LIDO: {path} ({stream.Length} bytes)");
                return Task.FromResult<Stream>(new MemoryStream(stream.ToArray()));
            }

            Console.WriteLine($"[MockStorage] ERRO: Caminho não encontrado -> {path}");
            throw new FileNotFoundException("Arquivo não encontrado no mock storage", path);
        }

        public Task DeleteAsync(string path)
        {
            _storage.TryRemove(path, out _);
            Console.WriteLine($"[MockStorage] REMOVIDO: {path}");
            return Task.CompletedTask;
        }
    }
}
