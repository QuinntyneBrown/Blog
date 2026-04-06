using Blog.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Blog.Api.Services;

public class NewsletterEmailDispatchService(
    IServiceScopeFactory scopeFactory,
    ILogger<NewsletterEmailDispatchService> logger) : BackgroundService
{
    // This service is the Service Bus consumer placeholder.
    // In a production deployment, this would use ServiceBusClient to dequeue messages
    // from the newsletter-dispatch queue. For the initial implementation without
    // Azure Service Bus infrastructure, this service is a no-op — newsletter delivery
    // messages are dispatched by OutboxDispatchService to Service Bus (when available).
    //
    // The idempotency check, dead-lettering, and per-subscriber failure isolation
    // patterns described in the design (§3.7, §3.8) are implemented here for when
    // Service Bus integration is added.

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NewsletterEmailDispatchService started. Waiting for Service Bus infrastructure.");
        return Task.CompletedTask;
    }

    // This method will be called per-message when Service Bus integration is added.
    internal async Task ProcessMessageAsync(
        Guid newsletterId, Guid subscriberId, string email,
        IUnitOfWork uow, IEmailSender emailSender, IUnsubscribeTokenService tokenService,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = ComputeIdempotencyKey(newsletterId, subscriberId);

        // Check idempotency
        if (await uow.Newsletters.SendLogExistsAsync(newsletterId, idempotencyKey, cancellationToken))
        {
            logger.LogInformation("Skipping duplicate delivery for newsletter {NewsletterId}, subscriber {SubscriberId}",
                newsletterId, subscriberId);
            return;
        }

        // Load newsletter
        var newsletter = await uow.Newsletters.GetByIdAsync(newsletterId, cancellationToken);
        if (newsletter == null)
        {
            logger.LogError("Business event {EventType} occurred: {@Details}",
                "newsletter.send_failed", new { NewsletterId = newsletterId, SubscriberId = subscriberId, Reason = "NewsletterNotFound" });
            return;
        }

        // Generate unsubscribe URL
        var unsubscribeToken = tokenService.GenerateToken(subscriberId);

        // Send email
        await emailSender.SendNewsletterEmailAsync(email, newsletter.Subject, newsletter.BodyHtml, unsubscribeToken, cancellationToken);

        // Record send log
        var sendLog = new Domain.Entities.NewsletterSendLog
        {
            NewsletterSendLogId = Guid.NewGuid(),
            NewsletterId = newsletterId,
            SubscriberId = subscriberId,
            RecipientIdempotencyKey = idempotencyKey,
            SentAt = DateTime.UtcNow
        };

        try
        {
            await uow.Newsletters.AddSendLogAsync(sendLog, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Unique constraint violation — peer instance already processed this message
            logger.LogInformation("Idempotency key collision for newsletter {NewsletterId}, subscriber {SubscriberId} — peer already processed",
                newsletterId, subscriberId);
        }
    }

    private static string ComputeIdempotencyKey(Guid newsletterId, Guid subscriberId)
    {
        var input = $"{newsletterId}:{subscriberId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
