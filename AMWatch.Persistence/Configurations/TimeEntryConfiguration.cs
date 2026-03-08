using AMWatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMWatch.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(x => x.EntryId);
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.Duration).IsRequired();
        builder.HasIndex(x => x.TaskId);
    }
}
