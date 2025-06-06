using System.IO;
using System.Threading.Tasks;

namespace TCUWatcher.Domain.Services
{
    /// <summary>
    /// Abstração genérica para salvar, ler e remover dados (arquivos, blobs,
    /// binários) em algum storage agnóstico (disco local, Azure Blob, S3, etc.).
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Salva o conteúdo do <paramref name="data"/> em um storage sob a
        /// chave <paramref name="key"/>. Retorna, opcionalmente, uma URL ou
        /// confirmação.
        /// </summary>
        /// <param name="key">Chave ou caminho único para o recurso.</param>
        /// <param name="data">Stream com os bytes a serem armazenados.</param>
        /// <returns>Uma string (pode ser a mesma key, ou uma URL pública, etc.).</returns>
        Task<string> SaveAsync(string key, Stream data);

        /// <summary>
        /// Lê do storage o conteúdo identificado por <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Chave ou caminho do recurso a ser lido.</param>
        /// <returns>Stream de leitura dos dados armazenados.</returns>
        Task<Stream> ReadAsync(string key);

        /// <summary>
        /// Remove do storage o recurso apontado por <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Chave ou caminho do recurso a ser deletado.</param>
        Task DeleteAsync(string key);
    }
}
