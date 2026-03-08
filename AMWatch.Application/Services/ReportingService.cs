using AMWatch.Domain.Interfaces;

namespace AMWatch.Application.Services;

public class ReportingService
{
    private readonly ITimeEntryRepository _timeRepository;

    public ReportingService(ITimeEntryRepository timeRepository)
    {
        _timeRepository = timeRepository;
    }

    public async Task<TimeSpan> GetDailyProductivityAsync(Guid taskId, DateTime date)
    {
        var entries = await _timeRepository.GetByTaskAsync(taskId);
        var filtered = entries.Where(e => e.StartTime.Date == date.Date);
        return TimeSpan.FromMinutes(filtered.Sum(e => e.Duration.TotalMinutes));
    }
}
