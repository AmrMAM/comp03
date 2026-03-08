using AMWatch.Domain.Enums;

namespace AMWatch.Application.Commands;

public class CreateTaskCommand
{
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public DateTime DueDate { get; set; }
}
