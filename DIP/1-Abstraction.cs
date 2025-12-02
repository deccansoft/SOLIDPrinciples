// ========== ABSTRACTIONS (Owned by High-Level Module) ==========

// Repository abstraction
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
    Task<Order> SaveAsync(Order order);
    Task UpdateAsync(Order order);
    Task<bool> ExistsAsync(int id);
}

// Payment processor abstraction
public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount);
    Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);
}

// Notification abstraction
public interface INotificationService
{
    Task SendAsync(Notification notification);
    Task SendBulkAsync(IEnumerable<Notification> notifications);
}

// Logging abstraction
public interface IAppLogger<T>
{
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception ex, string message, params object[] args);
}

// Date/Time abstraction (for testability)
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}

// Configuration abstraction
public interface IOrderSettings
{
    decimal TaxRate { get; }
    int MaxItemsPerOrder { get; }
    TimeSpan PaymentTimeout { get; }
    bool RequireEmailConfirmation { get; }
}
