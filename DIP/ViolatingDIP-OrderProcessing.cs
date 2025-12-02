// Low-level module: Concrete email sender
public class SmtpEmailSender
{
    public void SendEmail(string to, string subject, string body)
    {
        using var client = new SmtpClient("smtp.gmail.com", 587);
        client.Credentials = new NetworkCredential("user@gmail.com", "password");
        client.EnableSsl = true;

        var message = new MailMessage("noreply@myapp.com", to, subject, body);
        client.Send(message);
    }
}

// Low-level module: Concrete SQL Server repository
public class SqlServerOrderRepository
{
    private readonly string _connectionString = "Server=localhost;Database=Orders;...";

    public Order GetById(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        // Direct SQL queries
        return connection.QueryFirstOrDefault<Order>(
            "SELECT * FROM Orders WHERE Id = @Id",
            new { Id = id }
        );
    }

    public void Save(Order order)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Execute("INSERT INTO Orders...", order);
    }
}

// Low-level module: Concrete payment processor
public class StripePaymentProcessor
{
    public PaymentResult ProcessPayment(decimal amount, string cardToken)
    {
        var options = new ChargeCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = "usd",
            Source = cardToken,
        };

        var service = new ChargeService();
        var charge = service.Create(options);

        return new PaymentResult { Success = charge.Status == "succeeded" };
    }
}

// ❌ HIGH-LEVEL MODULE: Directly depends on ALL low-level concrete classes
public class OrderService
{
    private readonly SqlServerOrderRepository _orderRepository;
    private readonly StripePaymentProcessor _paymentProcessor;
    private readonly SmtpEmailSender _emailSender;

    public OrderService()
    {
        // ❌ Creating concrete dependencies - tightly coupled!
        _orderRepository = new SqlServerOrderRepository();
        _paymentProcessor = new StripePaymentProcessor();
        _emailSender = new SmtpEmailSender();
    }

    public OrderResult PlaceOrder(OrderRequest request)
    {
        // Process payment via Stripe (hardcoded)
        var paymentResult = _paymentProcessor.ProcessPayment(
            request.TotalAmount,
            request.CardToken
        );

        if (!paymentResult.Success)
            return new OrderResult { Success = false, Error = "Payment failed" };

        // Save to SQL Server (hardcoded)
        var order = new Order
        {
            CustomerId = request.CustomerId,
            TotalAmount = request.TotalAmount,
            Items = request.Items,
        };
        _orderRepository.Save(order);

        // Send email via SMTP (hardcoded)
        _emailSender.SendEmail(
            request.CustomerEmail,
            "Order Confirmation",
            $"Your order #{order.Id} has been placed!"
        );

        return new OrderResult { Success = true, OrderId = order.Id };
    }
}

/*
Problems:
OrderService (high-level) directly depends on SqlServerOrderRepository, StripePaymentProcessor, SmtpEmailSender (low-level)
Cannot switch to MongoDB, PayPal, or SendGrid without modifying OrderService
Cannot unit test without hitting real database, payment gateway, and email server
Changes in low-level modules ripple up to high-level modules
*/
