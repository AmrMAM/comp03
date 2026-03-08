using AMWatch.Domain.Entities;

namespace AMWatch.Domain.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId);
    Task<IEnumerable<TaskItem>> GetByUserAsync(Guid userId);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid taskId);
}
