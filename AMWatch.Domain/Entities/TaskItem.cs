using AMWatch.Domain.Enums;

namespace AMWatch.Domain.Entities;

public class TaskItem
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void EnsureValidDates()
    {
        if (DueDate < CreatedAt)
        {
            throw new InvalidOperationException("DueDate cannot be earlier than CreatedAt.");
        }
    }
}
