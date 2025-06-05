namespace TCUWatcher.Domain.Services;

public interface IStorageService
{
    Task<string> SaveAsync(string key, Stream data);
    Task<Stream> ReadAsync(string key);
    Task DeleteAsync(string key);
}
