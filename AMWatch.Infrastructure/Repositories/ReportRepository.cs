using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;
using AMWatch.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMWatch.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Report>> GetByUserAsync(Guid userId) => await _context.Reports.Where(r => r.UserId == userId).ToListAsync();

    public async Task AddAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();
    }
}
