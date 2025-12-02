[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateReport(
        [FromQuery] string format,
        [FromBody] ReportRequest request
    )
    {
        var result = await _reportService.GenerateReportAsync(format, request);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("formats")]
    public IActionResult GetSupportedFormats()
    {
        return Ok(_reportService.GetSupportedFormats());
    }
}
