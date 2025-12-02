//After: Using Polymorphism for Extension
public interface IReportGenerator
{
    string ReportType { get; }
    byte[] Generate(ReportData data);
    string ContentType { get; }
    string FileExtension { get; }
}

// Shared data model
public class ReportColumn
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Type DataType { get; set; } = typeof(string);
}

public class ReportData
{
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<ReportColumn> Columns { get; set; } = new();
    public List<Dictionary<string, object>> Rows { get; set; } = new();
}
