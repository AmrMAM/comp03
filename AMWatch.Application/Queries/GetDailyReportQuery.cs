namespace AMWatch.Application.Queries;

public class GetDailyReportQuery
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
}
