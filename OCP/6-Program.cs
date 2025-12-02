// Register all report generators
builder.Services.AddScoped<IReportGenerator, PdfReportGenerator>();
builder.Services.AddScoped<IReportGenerator, ExcelReportGenerator>();
builder.Services.AddScoped<IReportGenerator, CsvReportGenerator>();

// Register factory and service
builder.Services.AddScoped<IReportFactory, ReportFactory>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportDataBuilder, ReportDataBuilder>();