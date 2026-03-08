using AMWatch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMWatch.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(x => x.ReportId);
        builder.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
        builder.HasIndex(x => x.UserId);
    }
}
