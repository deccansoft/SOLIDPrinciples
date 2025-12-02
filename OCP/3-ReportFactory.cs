//Step 3: Create Report Factory (Strategy Pattern)
public interface IReportFactory
{
    IReportGenerator GetGenerator(string reportType);
    IEnumerable<string> GetAvailableReportTypes();
}

public class ReportFactory : IReportFactory
{
    private readonly IEnumerable<IReportGenerator> _generators;

    public ReportFactory(IEnumerable<IReportGenerator> generators)
    {
        _generators = generators;
    }

    public IReportGenerator GetGenerator(string reportType)
    {
        var generator = _generators.FirstOrDefault(g =>
            g.ReportType.Equals(reportType, StringComparison.OrdinalIgnoreCase)
        );

        if (generator == null)
            throw new NotSupportedException(
                $"Report type '{reportType}' is not supported. "
                    + $"Available types: {string.Join(", ", GetAvailableReportTypes())}"
            );

        return generator;
    }

    public IEnumerable<string> GetAvailableReportTypes()
    {
        return _generators.Select(g => g.ReportType);
    }
}
