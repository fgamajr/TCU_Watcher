using TCUWatcher.Domain.Entities;

namespace TCUWatcher.Domain.Repositories;

public interface ITranscriptSegmentRepository
{
    Task<IEnumerable<TranscriptSegment>> GetByProcessIdAsync(string processId);
    Task AddAsync(TranscriptSegment segment);
    Task UpdateAsync(TranscriptSegment segment);
}
