namespace TCUWatcher.Domain.Errors;

public sealed record NotFoundError(string ResourceType, string ResourceId)
    : DomainError("Resource.NotFound",
                  $"O recurso do tipo '{ResourceType}' com ID '{ResourceId}' não foi encontrado.");
