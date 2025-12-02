// PDF Report Generator
public class PdfReportGenerator : IReportGenerator
{
    public string ReportType => "PDF";
    public string ContentType => "application/pdf";
    public string FileExtension => ".pdf";

    public byte[] Generate(ReportData data)
    {
        using var stream = new MemoryStream();

        // Using QuestPDF or iTextSharp
        Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Header().Text(data.Title).FontSize(20).Bold();
                    page.Content()
                        .Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var col in data.Columns)
                                    columns.RelativeColumn();
                            });

                            // Header row
                            foreach (var col in data.Columns)
                                table.Cell().Text(col.DisplayName).Bold();

                            // Data rows
                            foreach (var row in data.Rows)
                            {
                                foreach (var col in data.Columns)
                                    table.Cell().Text(row[col.Name]?.ToString() ?? "");
                            }
                        });
                });
            })
            .GeneratePdf(stream);

        return stream.ToArray();
    }
}

// Excel Report Generator
public class ExcelReportGenerator : IReportGenerator
{
    public string ReportType => "Excel";
    public string ContentType =>
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => ".xlsx";

    public byte[] Generate(ReportData data)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(data.Title);

        // Header row
        for (int i = 0; i < data.Columns.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = data.Columns[i].DisplayName;
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Data rows
        for (int row = 0; row < data.Rows.Count; row++)
        {
            for (int col = 0; col < data.Columns.Count; col++)
            {
                var columnName = data.Columns[col].Name;
                worksheet.Cells[row + 2, col + 1].Value = data.Rows[row][columnName];
            }
        }

        worksheet.Cells.AutoFitColumns();
        return package.GetAsByteArray();
    }
}

// CSV Report Generator
public class CsvReportGenerator : IReportGenerator
{
    public string ReportType => "CSV";
    public string ContentType => "text/csv";
    public string FileExtension => ".csv";

    public byte[] Generate(ReportData data)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine(string.Join(",", data.Columns.Select(c => $"\"{c.DisplayName}\"")));

        // Data rows
        foreach (var row in data.Rows)
        {
            var values = data.Columns.Select(c => $"\"{row[c.Name]}\"");
            sb.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
