using System;

namespace TCUWatcher.Domain.Services
{
    public interface ITitleValidationService
    {
        bool IsRelevant(string title);
    }
}
