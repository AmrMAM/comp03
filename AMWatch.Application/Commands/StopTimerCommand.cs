namespace AMWatch.Application.Commands;

public class StopTimerCommand
{
    public Guid EntryId { get; set; }
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
}
