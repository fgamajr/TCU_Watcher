using System;

namespace TCUWatcher.Domain.Errors;

public sealed record ValidationError(string Description)
    : DomainError("Validation.Error", Description);