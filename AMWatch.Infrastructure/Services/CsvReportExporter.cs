using System.Text;

namespace AMWatch.Infrastructure.Services;

public class CsvReportExporter
{
    public string Export(string filePath, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row));
        }

        File.WriteAllText(filePath, builder.ToString());
        return filePath;
    }
}
