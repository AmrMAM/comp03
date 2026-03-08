using AMWatch.Application.Commands;
using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;

namespace AMWatch.Application.Services;

public class TimeTrackingService
{
    private readonly ITimeEntryRepository _timeRepository;

    public TimeTrackingService(ITimeEntryRepository timeRepository)
    {
        _timeRepository = timeRepository;
    }

    public async Task<Guid> StartTimerAsync(StartTimerCommand command)
    {
        var existing = await _timeRepository.GetActiveByUserAsync(command.UserId);
        if (existing is not null)
        {
            throw new InvalidOperationException("An active timer already exists for this user.");
        }

        var entry = new TimeEntry
        {
            EntryId = Guid.NewGuid(),
            TaskId = command.TaskId,
            StartTime = command.StartTime
        };

        await _timeRepository.AddAsync(entry);
        return entry.EntryId;
    }

    public async Task StopTimerAsync(StopTimerCommand command)
    {
        var entry = await _timeRepository.GetByIdAsync(command.EntryId) ?? throw new InvalidOperationException("Time entry not found.");
        entry.Stop(command.EndTime);
        await _timeRepository.UpdateAsync(entry);
    }
}
