using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;
using AMWatch.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMWatch.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context) => _context = context;

    public async Task<TaskItem?> GetByIdAsync(Guid taskId) => await _context.Tasks.FindAsync(taskId);

    public async Task<IEnumerable<TaskItem>> GetByUserAsync(Guid userId) => await _context.Tasks.Where(t => t.UserId == userId).ToListAsync();

    public async Task AddAsync(TaskItem task)
    {
        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid taskId)
    {
        var entity = await _context.Tasks.FindAsync(taskId);
        if (entity is null) return;
        _context.Tasks.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
