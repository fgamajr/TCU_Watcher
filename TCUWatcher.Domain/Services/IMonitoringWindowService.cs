using System.Threading.Tasks;

namespace TCUWatcher.Domain.Services;

public interface IMonitoringWindowService
{
    /// <summary>
    /// Verifica se o momento atual está dentro de alguma janela de monitoramento ativa.
    /// </summary>
    Task<bool> IsCurrentlyInActiveWindowAsync();
}