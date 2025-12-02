//Step 4: Report Service (Core Code - Never Changes)
public interface IReportService
{
    Task<ReportResult> GenerateReportAsync(string reportType, ReportRequest request);
    IEnumerable<string> GetSupportedFormats();
}

public class ReportResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

// This class NEVER needs modification when adding new report types
public class ReportService : IReportService
{
    private readonly IReportFactory _reportFactory;
    private readonly IReportDataBuilder _dataBuilder;

    public ReportService(IReportFactory reportFactory, IReportDataBuilder dataBuilder)
    {
        _reportFactory = reportFactory;
        _dataBuilder = dataBuilder;
    }

    public async Task<ReportResult> GenerateReportAsync(string reportType, ReportRequest request)
    {
        // Get appropriate generator via factory
        var generator = _reportFactory.GetGenerator(reportType);

        // Build report data
        var reportData = await _dataBuilder.BuildAsync(request);

        // Generate report using polymorphism
        var content = generator.Generate(reportData);

        return new ReportResult
        {
            Content = content,
            ContentType = generator.ContentType,
            FileName = $"{request.ReportName}_{DateTime.Now:yyyyMMdd}{generator.FileExtension}",
        };
    }

    public IEnumerable<string> GetSupportedFormats()
    {
        return _reportFactory.GetAvailableReportTypes();
    }
}
