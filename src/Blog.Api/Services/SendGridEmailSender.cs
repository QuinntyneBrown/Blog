using Microsoft.Extensions.Options;

namespace Blog.Api.Services;

public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class SendGridEmailSender(IOptions<SendGridOptions> options, ILogger<SendGridEmailSender> logger) : IEmailSender
{
    public async Task SendConfirmationEmailAsync(string email, string confirmUrl, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "newsletter.confirmation_email_sent", new { EmailHash = HashEmail(email) });

        // SendGrid implementation would go here.
        // For now, log the intent. The actual SendGrid NuGet integration
        // is deferred until infrastructure is provisioned.
        await Task.CompletedTask;
    }

    public async Task SendNewsletterEmailAsync(string email, string subject, string bodyHtml, string unsubscribeUrl, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "newsletter.email_dispatched", new { EmailHash = HashEmail(email), Subject = subject });

        // SendGrid implementation would go here.
        await Task.CompletedTask;
    }

    private static string HashEmail(string email)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant().Trim()));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
