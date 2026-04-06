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
                }
                else
                {
                    var delay = Math.Min(BaseDelaySeconds * Math.Pow(2, message.RetryCount - 1), MaxDelaySeconds);
                    var jitter = delay * (0.8 + Jitter.NextDouble() * 0.4); // ±20%
                    message.NextRetryAt = DateTime.UtcNow.AddSeconds(jitter);

                    logger.LogWarning("Business event {EventType} occurred: {@Details}",
                        "outbox.retry", new { message.OutboxMessageId, message.MessageType, message.RetryCount, NextRetryAt = message.NextRetryAt });
                }

                uow.Newsletters.UpdateOutboxMessage(message);
                await uow.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private static async Task DispatchMessageAsync(OutboxMessage message, IUnitOfWork uow, IEmailSender emailSender, CancellationToken cancellationToken)
    {
        switch (message.MessageType)
        {
            case "ConfirmationEmail":
                await DispatchConfirmationEmailAsync(message, uow, emailSender, cancellationToken);
                break;

            case "NewsletterDelivery":
                // Newsletter delivery messages would be enqueued to Azure Service Bus.
                // For initial implementation, log the dispatch intent.
                // The NewsletterEmailDispatchService will handle actual email sending
                // once Service Bus infrastructure is provisioned.
                break;

            default:
                throw new InvalidOperationException($"Unknown outbox message type: {message.MessageType}");
        }
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

        if (subscriber.TokenExpiresAt != null && subscriber.TokenExpiresAt < DateTime.UtcNow)
            return; // Token expired — don't send confusing email

        await emailSender.SendConfirmationEmailAsync(email, confirmUrl, cancellationToken);
    }
}
