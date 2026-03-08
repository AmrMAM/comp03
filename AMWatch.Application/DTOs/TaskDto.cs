using AMWatch.Domain.Enums;

namespace AMWatch.Application.DTOs;

public class TaskDto
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
