# AM Watch

AM Watch is a Clean Architecture starter implementation for a .NET 10 Todo + Time Tracker + Reporting system.

## Projects
- `AMWatch.Domain`: entities, enums, repository contracts.
- `AMWatch.Application`: use-case services, commands/queries, DTOs.
- `AMWatch.Persistence`: EF Core DbContext and entity configurations.
- `AMWatch.Infrastructure`: repository implementations, JWT, export services.
- `AMWatch.Presentation`: Blazor Server + MudBlazor UI.
- `AMWatch.Tests`: initial unit test scaffolding.

## Notes
This environment does not include the .NET SDK, so compile/test execution must be done in a machine with .NET installed.
