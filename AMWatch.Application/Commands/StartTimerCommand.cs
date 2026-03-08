namespace AMWatch.Application.Commands;

public class StartTimerCommand
{
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}
