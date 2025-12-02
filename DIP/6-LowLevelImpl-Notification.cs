// ========== NOTIFICATION IMPLEMENTATIONS ==========

// Email via SMTP
public class SmtpNotificationService : INotificationService
{
    private readonly SmtpSettings _settings;
    private readonly IAppLogger<SmtpNotificationService> _logger;

    public SmtpNotificationService(
        IOptions<SmtpSettings> settings,
        IAppLogger<SmtpNotificationService> logger
    )
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(Notification notification)
    {
        if (notification.Type != NotificationType.Email)
        {
            _logger.LogWarning("SMTP service only supports email notifications");
            return;
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port);
        client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        client.EnableSsl = _settings.UseSsl;

        var message = new MailMessage(_settings.FromAddress, notification.Recipient)
        {
            Subject = notification.Subject,
            Body = BuildEmailBody(notification),
            IsBodyHtml = true,
        };

        await client.SendMailAsync(message);
        _logger.LogInfo("Email sent to {Recipient}", notification.Recipient);
    }

    public async Task SendBulkAsync(IEnumerable<Notification> notifications)
    {
        foreach (var notification in notifications)
        {
            await SendAsync(notification);
        }
    }

    private string BuildEmailBody(Notification notification)
    {
        var body = notification.Body;
        foreach (var (key, value) in notification.TemplateData)
        {
            body = body.Replace($"{{{{{key}}}}}", value);
        }
        return body;
    }
}

// SendGrid Implementation
public class SendGridNotificationService : INotificationService
{
    private readonly SendGridClient _client;
    private readonly SendGridSettings _settings;
    private readonly IAppLogger<SendGridNotificationService> _logger;

    public SendGridNotificationService(
        IOptions<SendGridSettings> settings,
        IAppLogger<SendGridNotificationService> logger
    )
    {
        _settings = settings.Value;
        _client = new SendGridClient(_settings.ApiKey);
        _logger = logger;
    }

    public async Task SendAsync(Notification notification)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_settings.FromEmail, _settings.FromName),
            Subject = notification.Subject,
        };

        msg.AddTo(notification.Recipient);

        if (notification.TemplateData.Any() && !string.IsNullOrEmpty(_settings.TemplateId))
        {
            msg.SetTemplateId(_settings.TemplateId);
            msg.SetTemplateData(notification.TemplateData);
        }
        else
        {
            msg.HtmlContent = notification.Body;
        }

        var response = await _client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
            _logger.LogInfo("SendGrid email sent to {Recipient}", notification.Recipient);
        else
            _logger.LogWarning("SendGrid failed: {StatusCode}", response.StatusCode);
    }

    public async Task SendBulkAsync(IEnumerable<Notification> notifications)
    {
        var tasks = notifications.Select(SendAsync);
        await Task.WhenAll(tasks);
    }
}

// Multi-channel notification service (composite pattern)
public class MultiChannelNotificationService : INotificationService
{
    private readonly IEnumerable<INotificationService> _services;
    private readonly IAppLogger<MultiChannelNotificationService> _logger;

    public MultiChannelNotificationService(
        IEnumerable<INotificationService> services,
        IAppLogger<MultiChannelNotificationService> logger
    )
    {
        _services = services;
        _logger = logger;
    }

    public async Task SendAsync(Notification notification)
    {
        foreach (var service in _services)
        {
            try
            {
                await service.SendAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification service failed, trying next");
            }
        }
    }

    public async Task SendBulkAsync(IEnumerable<Notification> notifications)
    {
        foreach (var notification in notifications)
        {
            await SendAsync(notification);
        }
    }
}
