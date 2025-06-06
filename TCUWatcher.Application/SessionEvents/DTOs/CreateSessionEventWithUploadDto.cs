using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    /// <summary>
    /// DTO usado internamente no serviço de aplicação para tratar upload manual.
    /// Não contém IFormFile (isso fica na camada de API).
    /// </summary>
    public class CreateSessionEventWithUploadDto
    {
        public string Title { get; set; } = default!;
        public string StorageKey { get; set; } = default!;  // chave/caminho no storage
        public DateTime? StartedAt { get; set; }
    }
}
