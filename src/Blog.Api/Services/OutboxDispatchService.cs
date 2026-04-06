using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Blog.Api.Services;

public class OutboxOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxRetries { get; set; } = 5;
    public int RetentionDays { get; set; } = 7;
    public int BatchSize { get; set; } = 100;
}

public class OutboxDispatchService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatchService> logger) : BackgroundService
{
    private const double BaseDelaySeconds = 10;
    private const double MaxDelaySeconds = 900; // 15 minutes
    private static readonly Random Jitter = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox dispatch cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var messages = await uow.Newsletters.GetPendingOutboxMessagesAsync(options.Value.BatchSize, stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                await DispatchMessageAsync(message, uow, emailSender, stoppingToken);
                message.Status = OutboxMessageStatus.Completed;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;

                uow.Newsletters.UpdateOutboxMessage(message);
                await uow.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Business event {EventType} occurred: {@Details}",
                    "outbox.dispatched", new { message.OutboxMessageId, message.MessageType });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= options.Value.MaxRetries)
                {
                    message.Status = OutboxMessageStatus.DeadLettered;
                    message.ProcessedAt = DateTime.UtcNow;

                    logger.LogError("Business event {EventType} occurred: {@Details}",
                        "outbox.dead_lettered", new { message.OutboxMessageId, message.MessageType, message.RetryCount, Error = ex.Message });

                    // Emit newsletter-specific dead-letter event (design §3.8 / §7)
                    if (message.MessageType == "NewsletterDelivery")
                    {
                        logger.LogError("Business event {EventType} occurred: {@Details}",
                            "newsletter.send_failed", new { message.OutboxMessageId, Error = ex.Message });
                    }
                }
                else
                {
                    var delay = Math.Min(BaseDelaySeconds * Math.Pow(2, message.RetryCount - 1), MaxDelaySeconds);
                    var jitter = delay * (0.8 + Jitter.NextDouble() * 0.4); // ±20%
                    message.NextRetryAt = DateTime.UtcNow.AddSeconds(jitter);

                    logger.LogWarning("Business event {EventType} occurred: {@Details}",
                        "outbox.retry", new { message.OutboxMessageId, message.MessageType, message.RetryCount, NextRetryAt = message.NextRetryAt });

                    // Emit newsletter-specific transient failure event at Warning (design §7)
                    if (message.MessageType == "NewsletterDelivery")
                    {
                        logger.LogWarning("Business event {EventType} occurred: {@Details}",
                            "newsletter.send_failed", new { message.OutboxMessageId, Error = ex.Message });
                    }
                }

                uow.Newsletters.UpdateOutboxMessage(message);
                await uow.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private async Task DispatchMessageAsync(OutboxMessage message, IUnitOfWork uow, IEmailSender emailSender, CancellationToken cancellationToken)
    {
        switch (message.MessageType)
        {
            case "ConfirmationEmail":
                await DispatchConfirmationEmailAsync(message, uow, emailSender, cancellationToken);
                break;

            case "NewsletterDelivery":
                // Design §3.9: route to Azure Service Bus. Until Service Bus infrastructure
                // is provisioned, dispatch directly using the same idempotency logic from §3.8.
                await DispatchNewsletterDeliveryAsync(message, uow, emailSender, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unknown outbox message type: {message.MessageType}");
        }
    }

    private async Task DispatchNewsletterDeliveryAsync(OutboxMessage message, IUnitOfWork uow, IEmailSender emailSender, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
        var newsletterId = payload.GetProperty("newsletterId").GetGuid();
        var subscriberId = payload.GetProperty("subscriberId").GetGuid();
        var email = payload.GetProperty("email").GetString()!;

        // Idempotency check (design §3.8)
        var idempotencyKey = ComputeIdempotencyKey(newsletterId, subscriberId);
        if (await uow.Newsletters.SendLogExistsAsync(newsletterId, idempotencyKey, cancellationToken))
            return; // Already sent

        // Load newsletter
        var newsletter = await uow.Newsletters.GetByIdAsync(newsletterId, cancellationToken);
        if (newsletter == null)
        {
            logger.LogError("Business event {EventType} occurred: {@Details}",
                "newsletter.send_failed", new { NewsletterId = newsletterId, SubscriberId = subscriberId, Reason = "NewsletterNotFound" });
            throw new InvalidOperationException($"Newsletter {newsletterId} not found — dead-letter this message.");
        }

        // Generate unsubscribe URL using HMAC token (design §3.10)
        var tokenService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IUnsubscribeTokenService>();
        var unsubscribeToken = tokenService.GenerateToken(subscriberId);

        // Send email
        await emailSender.SendNewsletterEmailAsync(email, newsletter.Subject, newsletter.BodyHtml, unsubscribeToken, cancellationToken);

        // Record send log — must complete before message is marked done (design §3.8)
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
            // Unique constraint violation — peer instance already processed
        }
    }

    private static string ComputeIdempotencyKey(Guid newsletterId, Guid subscriberId)
    {
        var input = $"{newsletterId}:{subscriberId}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task DispatchConfirmationEmailAsync(OutboxMessage message, IUnitOfWork uow, IEmailSender emailSender, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
        var subscriberId = payload.GetProperty("subscriberId").GetGuid();
        var email = payload.GetProperty("email").GetString()!;
        var confirmUrl = payload.GetProperty("confirmUrl").GetString()!;

        // Check if subscriber still needs confirmation
        var subscriber = await uow.Newsletters.GetSubscriberByIdAsync(subscriberId, cancellationToken);
        if (subscriber == null)
            return; // Subscriber deleted

        if (subscriber.ConfirmationTokenHash == null)
            return; // Already confirmed

        // Expired-token guard (design §3.9.6): treat null TokenExpiresAt as expired
        // (defensive against malformed rows where Confirmed=false with TokenExpiresAt=null)
        if (subscriber.TokenExpiresAt == null || subscriber.TokenExpiresAt < DateTime.UtcNow)
            return; // Token expired or already used — don't send confusing email

        await emailSender.SendConfirmationEmailAsync(email, confirmUrl, cancellationToken);
    }
}
