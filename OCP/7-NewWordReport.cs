// Just add this NEW class - no existing code modified!
public class WordReportGenerator : IReportGenerator
{
    public string ReportType => "Word";
    public string ContentType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public string FileExtension => ".docx";

    public byte[] Generate(ReportData data)
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body;

        // Add title
        body.AppendChild(new Paragraph(new Run(new Text(data.Title))));

        // Add table with data
        var table = new Table();
        // ... table building logic
        body.AppendChild(table);

        document.Save();
        return stream.ToArray();
    }
}

// Just register it in Program.cs - that's all!
//builder.Services.AddScoped<IReportGenerator, WordReportGenerator>();