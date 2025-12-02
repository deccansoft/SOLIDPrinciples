public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IPaymentProcessor> _mockPaymentProcessor;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IAppLogger<OrderService>> _mockLogger;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<IOrderSettings> _mockSettings;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockPaymentProcessor = new Mock<IPaymentProcessor>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<IAppLogger<OrderService>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockSettings = new Mock<IOrderSettings>();

        // Setup default settings
        _mockSettings.Setup(s => s.TaxRate).Returns(0.18m);
        _mockSettings.Setup(s => s.MaxItemsPerOrder).Returns(50);
        _mockSettings.Setup(s => s.RequireEmailConfirmation).Returns(true);
        _mockDateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2024, 1, 15, 10, 0, 0));

        _orderService = new OrderService(
            _mockOrderRepo.Object,
            _mockPaymentProcessor.Object,
            _mockNotificationService.Object,
            _mockLogger.Object,
            _mockDateTimeProvider.Object,
            _mockSettings.Object
        );
    }

    [Fact]
    public async Task PlaceOrderAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            CustomerId = 1,
            CustomerEmail = "customer@example.com",
            CardToken = "tok_visa",
            Items = new List<OrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Product 1",
                    Quantity = 2,
                    UnitPrice = 100,
                },
            },
        };

        _mockOrderRepo
            .Setup(r => r.SaveAsync(It.IsAny<Order>()))
            .ReturnsAsync(
                (Order o) =>
                {
                    o.Id = 1;
                    return o;
                }
            );

        _mockPaymentProcessor
            .Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "txn_123" });

        // Act
        var result = await _orderService.PlaceOrderAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.OrderId);
        Assert.Equal(OrderStatus.Paid, result.Status);

        _mockNotificationService.Verify(n => n.SendAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenPaymentFails_ReturnsCancelledOrder()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            CustomerId = 1,
            CustomerEmail = "customer@example.com",
            CardToken = "tok_declined",
            Items = new List<OrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Product 1",
                    Quantity = 1,
                    UnitPrice = 100,
                },
            },
        };

        _mockOrderRepo
            .Setup(r => r.SaveAsync(It.IsAny<Order>()))
            .ReturnsAsync(
                (Order o) =>
                {
                    o.Id = 1;
                    return o;
                }
            );

        _mockPaymentProcessor
            .Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult { Success = false, ErrorMessage = "Card declined" });

        // Act
        var result = await _orderService.PlaceOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Payment failed", result.Error);
        Assert.Equal(OrderStatus.Cancelled, result.Status);

        // Notification should NOT be sent for failed orders
        _mockNotificationService.Verify(n => n.SendAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithEmptyItems_ReturnsError()
    {
        // Arrange
        var request = new PlaceOrderRequest
        {
            CustomerId = 1,
            CustomerEmail = "customer@example.com",
            Items = new List<OrderItemRequest>(), // Empty!
        };

        // Act
        var result = await _orderService.PlaceOrderAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("at least one item", result.Error);
    }

    [Fact]
    public async Task RefundOrderAsync_WithValidOrder_ProcessesRefund()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            CustomerId = 1,
            CustomerEmail = "customer@example.com",
            Status = OrderStatus.Paid,
            TotalAmount = 118,
            PaymentTransactionId = "txn_123",
        };

        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _mockPaymentProcessor
            .Setup(p => p.RefundPaymentAsync("txn_123", 118))
            .ReturnsAsync(new RefundResult { Success = true, RefundId = "ref_123" });

        // Act
        var result = await _orderService.RefundOrderAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Refunded, result.Status);
        _mockNotificationService.Verify(
            n => n.SendAsync(It.Is<Notification>(n => n.Subject.Contains("Refund"))),
            Times.Once
        );
    }
}
