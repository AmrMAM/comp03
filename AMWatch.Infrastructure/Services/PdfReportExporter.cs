namespace AMWatch.Infrastructure.Services;

public class PdfReportExporter
{
    public string Export(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
