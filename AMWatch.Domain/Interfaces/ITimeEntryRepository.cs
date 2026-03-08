using AMWatch.Domain.Entities;

namespace AMWatch.Domain.Interfaces;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetByIdAsync(Guid entryId);
    Task<IEnumerable<TimeEntry>> GetByTaskAsync(Guid taskId);
    Task<TimeEntry?> GetActiveByUserAsync(Guid userId);
    Task AddAsync(TimeEntry entry);
    Task UpdateAsync(TimeEntry entry);
}
