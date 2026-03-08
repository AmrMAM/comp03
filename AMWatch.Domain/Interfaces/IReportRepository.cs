using AMWatch.Domain.Entities;

namespace AMWatch.Domain.Interfaces;

public interface IReportRepository
{
    Task<IEnumerable<Report>> GetByUserAsync(Guid userId);
    Task AddAsync(Report report);
}
