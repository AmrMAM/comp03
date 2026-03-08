using AMWatch.Application.Commands;

namespace AMWatch.Application.Validators;

public static class CreateTaskCommandValidator
{
    public static void Validate(CreateTaskCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ArgumentException("Task title is required.", nameof(command.Title));
        }
    }
}
