using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using TCUWatcher.Domain.Services; // <-- CORREÇÃO AQUI

namespace TCUWatcher.Infrastructure.Storage
{
    /// <summary>
    /// Implementação em memória de IStorageService para desenvolvimento.
    /// </summary>
    public class MockStorageService : IStorageService
    {
        private static readonly ConcurrentDictionary<string, byte[]> _store
            = new ConcurrentDictionary<string, byte[]>();

        public Task<string> SaveAsync(string key, Stream data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            using var ms = new MemoryStream();
            data.CopyTo(ms);
            _store[key] = ms.ToArray();
            return Task.FromResult(key);
        }

        public Task<Stream> ReadAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (!_store.TryGetValue(key, out var bytes))
                throw new FileNotFoundException($"Chave '{key}' não encontrada no MockStorageService.");

            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        public Task DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}