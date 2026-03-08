namespace AMWatch.Domain.Entities;

public class TimeEntry
{
    public Guid EntryId { get; set; }
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }

    public void Stop(DateTime endTime)
    {
        if (endTime <= StartTime)
        {
            throw new InvalidOperationException("EndTime must be greater than StartTime.");
        }

        EndTime = endTime;
        Duration = EndTime.Value - StartTime;
    }
}
