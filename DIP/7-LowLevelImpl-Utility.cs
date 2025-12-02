// ========== UTILITY IMPLEMENTATIONS ==========

// Logger wrapper
public class AppLogger<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;

    public AppLogger(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void LogInfo(string message, params object[] args) =>
        _logger.LogInformation(message, args);

    public void LogWarning(string message, params object[] args) =>
        _logger.LogWarning(message, args);

    public void LogError(Exception ex, string message, params object[] args) =>
        _logger.LogError(ex, message, args);
}

// DateTime provider
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
}

// Settings from configuration
public class OrderSettings : IOrderSettings
{
    public decimal TaxRate { get; set; } = 0.18m; // 18% GST
    public int MaxItemsPerOrder { get; set; } = 50;
    public TimeSpan PaymentTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool RequireEmailConfirmation { get; set; } = true;
}
