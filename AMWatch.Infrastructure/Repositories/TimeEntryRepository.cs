using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;
using AMWatch.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMWatch.Infrastructure.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly ApplicationDbContext _context;

    public TimeEntryRepository(ApplicationDbContext context) => _context = context;

    public Task<TimeEntry?> GetByIdAsync(Guid entryId) => _context.TimeEntries.FindAsync(entryId).AsTask();

    public async Task<IEnumerable<TimeEntry>> GetByTaskAsync(Guid taskId) => await _context.TimeEntries.Where(t => t.TaskId == taskId).ToListAsync();

    public async Task<TimeEntry?> GetActiveByUserAsync(Guid userId)
    {
        var taskIds = await _context.Tasks.Where(t => t.UserId == userId).Select(t => t.TaskId).ToListAsync();
        return await _context.TimeEntries.FirstOrDefaultAsync(t => taskIds.Contains(t.TaskId) && t.EndTime == null);
    }

    public async Task AddAsync(TimeEntry entry)
    {
        await _context.TimeEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TimeEntry entry)
    {
        _context.TimeEntries.Update(entry);
        await _context.SaveChangesAsync();
    }
}
