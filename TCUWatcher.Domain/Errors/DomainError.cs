namespace TCUWatcher.Domain.Errors;

public abstract record DomainError(string Code, string Message);
