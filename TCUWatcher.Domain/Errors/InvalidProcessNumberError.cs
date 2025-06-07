namespace TCUWatcher.Domain.Errors;

public sealed record InvalidProcessNumberError(string Number)
    : DomainError("ProcessNumber.Invalid",
                  $"Número de processo inválido: {Number}");
