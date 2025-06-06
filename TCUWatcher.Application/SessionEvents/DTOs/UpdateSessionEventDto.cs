// TCUWatcher.Application/SessionEvents/DTOs/UpdateSessionEventDto.cs
using System;

namespace TCUWatcher.Application.SessionEvents.DTOs
{
    /// <summary>
    /// DTO usado para atualizar apenas os campos 'IsLive' e 'EndedAt' de um SessionEvent.
    /// </summary>
    public class UpdateSessionEventDto
    {
        /// <summary>
        /// Indica se o evento ainda está ao vivo. Se null, não altera.
        /// </summary>
        public bool? IsLive { get; set; }

        /// <summary>
        /// Marca o horário de término da sessão. Se null, não altera.
        /// </summary>
        public DateTime? EndedAt { get; set; }
    }
}
