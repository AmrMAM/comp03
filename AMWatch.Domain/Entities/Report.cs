using AMWatch.Domain.Enums;

namespace AMWatch.Domain.Entities;

public class Report
{
    public Guid ReportId { get; set; }
    public Guid UserId { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    public ReportType ReportType { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
