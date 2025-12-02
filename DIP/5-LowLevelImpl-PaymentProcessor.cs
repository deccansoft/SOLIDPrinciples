// ========== PAYMENT PROCESSOR IMPLEMENTATIONS ==========

// Stripe Implementation
public class StripePaymentProcessor : IPaymentProcessor
{
    private readonly StripeClient _client;
    private readonly IAppLogger<StripePaymentProcessor> _logger;

    public StripePaymentProcessor(
        IOptions<StripeSettings> settings,
        IAppLogger<StripePaymentProcessor> logger
    )
    {
        _client = new StripeClient(settings.Value.SecretKey);
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = request.Currency.ToLower(),
                PaymentMethod = request.CardToken,
                Confirm = true,
                Description = request.Description,
                Metadata = request.Metadata,
            };

            var service = new PaymentIntentService(_client);
            var intent = await service.CreateAsync(options);

            return new PaymentResult
            {
                Success = intent.Status == "succeeded",
                TransactionId = intent.Id,
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment failed");
            return new PaymentResult
            {
                Success = false,
                ErrorCode = ex.StripeError.Code,
                ErrorMessage = ex.StripeError.Message,
            };
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Amount = (long)(amount * 100),
            };

            var service = new RefundService(_client);
            var refund = await service.CreateAsync(options);

            return new RefundResult
            {
                Success = refund.Status == "succeeded",
                RefundId = refund.Id,
            };
        }
        catch (StripeException ex)
        {
            return new RefundResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        var service = new PaymentIntentService(_client);
        var intent = await service.GetAsync(transactionId);

        return intent.Status switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "processing" => PaymentStatus.Processing,
            "requires_payment_method" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending,
        };
    }
}

// Razorpay Implementation (for Indian payments)
public class RazorpayPaymentProcessor : IPaymentProcessor
{
    private readonly RazorpayClient _client;
    private readonly IAppLogger<RazorpayPaymentProcessor> _logger;

    public RazorpayPaymentProcessor(
        IOptions<RazorpaySettings> settings,
        IAppLogger<RazorpayPaymentProcessor> logger
    )
    {
        _client = new RazorpayClient(settings.Value.KeyId, settings.Value.KeySecret);
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            var options = new Dictionary<string, object>
            {
                { "amount", (int)(request.Amount * 100) },
                { "currency", request.Currency },
                { "receipt", request.Description },
                { "notes", request.Metadata },
            };

            var order = _client.Order.Create(options);

            // In Razorpay, payment confirmation happens client-side
            // This creates an order that client uses to initiate payment
            return new PaymentResult { Success = true, TransactionId = order["id"].ToString() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay payment failed");
            return new PaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            var refund = _client
                .Payment.Fetch(transactionId)
                .Refund(new Dictionary<string, object> { { "amount", (int)(amount * 100) } });

            return new RefundResult { Success = true, RefundId = refund["id"].ToString() };
        }
        catch (Exception ex)
        {
            return new RefundResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        var payment = _client.Payment.Fetch(transactionId);
        var status = payment["status"].ToString();

        return Task.FromResult(
            status switch
            {
                "captured" => PaymentStatus.Succeeded,
                "authorized" => PaymentStatus.Processing,
                "failed" => PaymentStatus.Failed,
                "refunded" => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending,
            }
        );
    }
}
