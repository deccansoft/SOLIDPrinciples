// Program.cs - Configure DI based on environment/requirements

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURATION =====
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<RazorpaySettings>(builder.Configuration.GetSection("Razorpay"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
builder.Services.Configure<OrderSettings>(builder.Configuration.GetSection("OrderSettings"));

// ===== UTILITY SERVICES =====
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));
builder.Services.AddSingleton<IOrderSettings>(sp =>
    sp.GetRequiredService<IOptions<OrderSettings>>().Value
);

// ===== DATABASE (Choose ONE) =====
// Option A: SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"))
);
builder.Services.AddScoped<IOrderRepository, SqlServerOrderRepository>();

// Option B: MongoDB (comment out SQL Server, uncomment this)
// builder.Services.AddSingleton<IMongoClient>(sp =>
//     new MongoClient(builder.Configuration.GetConnectionString("MongoDB")));
// builder.Services.AddScoped<IOrderRepository, MongoOrderRepository>();

// ===== PAYMENT PROCESSOR (Choose based on region/requirement) =====
var paymentProvider = builder.Configuration.GetValue<string>("PaymentProvider");

if (paymentProvider == "Razorpay")
{
    builder.Services.AddScoped<IPaymentProcessor, RazorpayPaymentProcessor>();
}
else
{
    builder.Services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();
}

// ===== NOTIFICATION SERVICE (Choose ONE or use multi-channel) =====
var notificationProvider = builder.Configuration.GetValue<string>("NotificationProvider");

switch (notificationProvider)
{
    case "SendGrid":
        builder.Services.AddScoped<INotificationService, SendGridNotificationService>();
        break;
    case "MultiChannel":
        builder.Services.AddScoped<SmtpNotificationService>();
        builder.Services.AddScoped<SendGridNotificationService>();
        builder.Services.AddScoped<INotificationService>(sp => new MultiChannelNotificationService(
            new INotificationService[]
            {
                sp.GetRequiredService<SendGridNotificationService>(),
                sp.GetRequiredService<SmtpNotificationService>(),
            },
            sp.GetRequiredService<IAppLogger<MultiChannelNotificationService>>()
        ));
        break;
    default:
        builder.Services.AddScoped<INotificationService, SmtpNotificationService>();
        break;
}

// ===== HIGH-LEVEL SERVICE =====
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();
