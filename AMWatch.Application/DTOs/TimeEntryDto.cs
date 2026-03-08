namespace AMWatch.Application.DTOs;

public class TimeEntryDto
{
    public Guid EntryId { get; set; }
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
}
