using System;

namespace TCUWatcher.Domain.Errors;

public sealed record LiveAlreadyEndedError(Guid SessionId)
    : DomainError("Session.LiveAlreadyEnded",
                  $"A sessão {SessionId} já terminou.");
