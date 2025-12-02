public class ReportGenerator
{
    public byte[] GenerateReport(string reportType, ReportData data)
    {
        switch (reportType)
        {
            case "PDF":
                // PDF generation logic
                return GeneratePdfReport(data);
            case "Excel":
                // Excel generation logic
                return GenerateExcelReport(data);
            case "CSV":
                // CSV generation logic
                return GenerateCsvReport(data);
            // To add Word report, we must MODIFY this class
            // case "Word":
            //     return GenerateWordReport(data);
            default:
                throw new NotSupportedException($"Report type '{reportType}' not supported");
        }
    }

    private byte[] GeneratePdfReport(ReportData data)
    {
        throw new NotImplementedException();
    }

    private byte[] GenerateExcelReport(ReportData data)
    {
        throw new NotImplementedException();
    }

    private byte[] GenerateCsvReport(ReportData data)
    {
        throw new NotImplementedException();
    }
}
/*
Problems:
Adding new report types requires modifying ReportGenerator
Risk of breaking existing functionality
Class grows larger with each new report type
*/
