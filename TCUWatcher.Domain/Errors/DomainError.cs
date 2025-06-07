namespace TCUWatcher.Domain.Errors;

public abstract record DomainError(string Code, string Message);

public sealed record InvalidProcessNumberError(string Number)
    : DomainError("ProcessNumber.Invalid",
                  $"Número de processo inválido: {Number}");

public sealed record LiveAlreadyEndedError(Guid SessionId)
    : DomainError("Session.LiveAlreadyEnded",
                  $"A sessão {SessionId} já terminou.");
