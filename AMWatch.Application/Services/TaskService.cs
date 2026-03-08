using AMWatch.Application.Commands;
using AMWatch.Application.DTOs;
using AMWatch.Application.Validators;
using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;

namespace AMWatch.Application.Services;

public class TaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<Guid> CreateTaskAsync(CreateTaskCommand command)
    {
        CreateTaskCommandValidator.Validate(command);
        var task = new TaskItem
        {
            TaskId = Guid.NewGuid(),
            UserId = command.UserId,
            CategoryId = command.CategoryId,
            Title = command.Title,
            Description = command.Description,
            Priority = command.Priority,
            DueDate = command.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        task.EnsureValidDates();
        await _taskRepository.AddAsync(task);
        return task.TaskId;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync(Guid userId)
    {
        var tasks = await _taskRepository.GetByUserAsync(userId);
        return tasks.Select(t => new TaskDto
        {
            TaskId = t.TaskId,
            UserId = t.UserId,
            CategoryId = t.CategoryId,
            Title = t.Title,
            Description = t.Description,
            Priority = t.Priority,
            Status = t.Status,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt
        });
    }
}
