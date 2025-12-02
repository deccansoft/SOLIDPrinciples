// ========== HIGH-LEVEL MODULE: OrderService ==========
// Depends ONLY on abstractions - knows nothing about SQL, Stripe, SMTP, etc.

public interface IOrderService
{
    Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request);
    Task<OrderResult> CancelOrderAsync(int orderId);
    Task<OrderResult> RefundOrderAsync(int orderId);
    Task<Order?> GetOrderAsync(int orderId);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly INotificationService _notificationService;
    private readonly IAppLogger<OrderService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOrderSettings _settings;

    // ✅ All dependencies are ABSTRACTIONS injected via constructor
    public OrderService(
        IOrderRepository orderRepository,
        IPaymentProcessor paymentProcessor,
        INotificationService notificationService,
        IAppLogger<OrderService> logger,
        IDateTimeProvider dateTimeProvider,
        IOrderSettings settings
    )
    {
        _orderRepository = orderRepository;
        _paymentProcessor = paymentProcessor;
        _notificationService = notificationService;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings;
    }

    public async Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request)
    {
        _logger.LogInfo("Placing order for customer {CustomerId}", request.CustomerId);

        // Validate request
        if (request.Items.Count == 0)
            return new OrderResult
            {
                Success = false,
                Error = "Order must contain at least one item",
            };

        if (request.Items.Count > _settings.MaxItemsPerOrder)
            return new OrderResult
            {
                Success = false,
                Error = $"Maximum {_settings.MaxItemsPerOrder} items allowed",
            };

        // Calculate totals
        var subTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = subTotal * _settings.TaxRate;
        var totalAmount = subTotal + taxAmount;

        // Create order entity
        var order = new Order
        {
            CustomerId = request.CustomerId,
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            CreatedAt = _dateTimeProvider.UtcNow,
            Items = request
                .Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                })
                .ToList(),
        };

        // Save initial order
        order = await _orderRepository.SaveAsync(order);
        _logger.LogInfo("Order {OrderId} created, processing payment", order.Id);

        // Process payment (via abstraction - could be Stripe, PayPal, Razorpay, etc.)
        order.Status = OrderStatus.PaymentProcessing;
        await _orderRepository.UpdateAsync(order);

        var paymentRequest = new PaymentRequest
        {
            Amount = totalAmount,
            Currency = "INR",
            CardToken = request.CardToken,
            Description = $"Order #{order.Id}",
            Metadata = new Dictionary<string, string>
            {
                ["OrderId"] = order.Id.ToString(),
                ["CustomerId"] = request.CustomerId.ToString(),
            },
        };

        var paymentResult = await _paymentProcessor.ProcessPaymentAsync(paymentRequest);

        if (!paymentResult.Success)
        {
            _logger.LogWarning(
                "Payment failed for order {OrderId}: {Error}",
                order.Id,
                paymentResult.ErrorMessage
            );

            order.Status = OrderStatus.Cancelled;
            await _orderRepository.UpdateAsync(order);

            return new OrderResult
            {
                Success = false,
                OrderId = order.Id,
                Error = $"Payment failed: {paymentResult.ErrorMessage}",
                Status = OrderStatus.Cancelled,
            };
        }

        // Update order with payment info
        order.Status = OrderStatus.Paid;
        order.PaymentTransactionId = paymentResult.TransactionId;
        order.PaidAt = _dateTimeProvider.UtcNow;
        await _orderRepository.UpdateAsync(order);

        _logger.LogInfo(
            "Order {OrderId} paid successfully, transaction: {TransactionId}",
            order.Id,
            paymentResult.TransactionId
        );

        // Send confirmation notification (via abstraction - could be email, SMS, push, etc.)
        if (_settings.RequireEmailConfirmation)
        {
            await _notificationService.SendAsync(
                new Notification
                {
                    Recipient = request.CustomerEmail,
                    Subject = $"Order Confirmation - #{order.Id}",
                    Type = NotificationType.Email,
                    TemplateData = new Dictionary<string, string>
                    {
                        ["OrderId"] = order.Id.ToString(),
                        ["TotalAmount"] = totalAmount.ToString("C"),
                        ["ItemCount"] = order.Items.Count.ToString(),
                    },
                }
            );
        }

        return new OrderResult
        {
            Success = true,
            OrderId = order.Id,
            Status = OrderStatus.Paid,
        };
    }

    public async Task<OrderResult> CancelOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
            return new OrderResult { Success = false, Error = "Order not found" };

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            return new OrderResult
            {
                Success = false,
                Error = "Cannot cancel shipped/delivered orders",
            };

        // If payment was processed, initiate refund
        if (order.Status == OrderStatus.Paid && !string.IsNullOrEmpty(order.PaymentTransactionId))
        {
            return await RefundOrderAsync(orderId);
        }

        order.Status = OrderStatus.Cancelled;
        await _orderRepository.UpdateAsync(order);

        await _notificationService.SendAsync(
            new Notification
            {
                Recipient = order.CustomerEmail,
                Subject = $"Order Cancelled - #{order.Id}",
                Type = NotificationType.Email,
            }
        );

        return new OrderResult
        {
            Success = true,
            OrderId = orderId,
            Status = OrderStatus.Cancelled,
        };
    }

    public async Task<OrderResult> RefundOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
            return new OrderResult { Success = false, Error = "Order not found" };

        if (string.IsNullOrEmpty(order.PaymentTransactionId))
            return new OrderResult { Success = false, Error = "No payment to refund" };

        var refundResult = await _paymentProcessor.RefundPaymentAsync(
            order.PaymentTransactionId,
            order.TotalAmount
        );

        if (!refundResult.Success)
        {
            _logger.LogError(
                null!,
                "Refund failed for order {OrderId}: {Error}",
                orderId,
                refundResult.ErrorMessage
            );
            return new OrderResult { Success = false, Error = refundResult.ErrorMessage };
        }

        order.Status = OrderStatus.Refunded;
        await _orderRepository.UpdateAsync(order);

        await _notificationService.SendAsync(
            new Notification
            {
                Recipient = order.CustomerEmail,
                Subject = $"Refund Processed - #{order.Id}",
                Type = NotificationType.Email,
            }
        );

        return new OrderResult
        {
            Success = true,
            OrderId = orderId,
            Status = OrderStatus.Refunded,
        };
    }

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        return await _orderRepository.GetByIdAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId)
    {
        return await _orderRepository.GetByCustomerIdAsync(customerId);
    }
}
