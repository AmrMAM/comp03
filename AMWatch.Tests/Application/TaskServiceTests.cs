using AMWatch.Application.Commands;
using AMWatch.Application.Services;
using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;
using FluentAssertions;

namespace AMWatch.Tests.Application;

public class TaskServiceTests
{
    [Fact]
    public async Task CreateTask_ShouldReturnTaskId()
    {
        var repo = new FakeTaskRepository();
        var service = new TaskService(repo);

        var id = await service.CreateTaskAsync(new CreateTaskCommand
        {
            UserId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Title = "Test Task",
            DueDate = DateTime.UtcNow.AddDays(1)
        });

        id.Should().NotBeEmpty();
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public Task AddAsync(TaskItem task) => Task.CompletedTask;
        public Task DeleteAsync(Guid taskId) => Task.CompletedTask;
        public Task<TaskItem?> GetByIdAsync(Guid taskId) => Task.FromResult<TaskItem?>(null);
        public Task<IEnumerable<TaskItem>> GetByUserAsync(Guid userId) => Task.FromResult<IEnumerable<TaskItem>>(Array.Empty<TaskItem>());
        public Task UpdateAsync(TaskItem task) => Task.CompletedTask;
    }
}
