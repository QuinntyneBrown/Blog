using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace Blog.Api.Features.Subscriptions.Commands;

public record SubscribeCommand(string Email) : IRequest;

public class SubscribeCommandValidator : AbstractValidator<SubscribeCommand>
{
    public SubscribeCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
    }
}

public class SubscribeCommandHandler(
    IUnitOfWork uow,
    IConfiguration configuration,
    ILogger<SubscribeCommandHandler> logger) : IRequestHandler<SubscribeCommand>
{
    public async Task Handle(SubscribeCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var existing = await uow.Newsletters.GetSubscriberByEmailAsync(normalizedEmail, cancellationToken);

        if (existing != null)
        {
            // Already confirmed and active — return silently (prevents enumeration)
            if (existing.Confirmed && existing.IsActive)
                return;

            // Resubscribe path: reactivate inactive subscriber
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.Confirmed = false;
                existing.ResubscribedAt = now;
                existing.UpdatedAt = now;
            }

            // Generate new confirmation token (for both resubscribe and unconfirmed re-request)
            var (rawToken, tokenHash) = GenerateConfirmationToken();
            existing.ConfirmationTokenHash = tokenHash;
            existing.TokenExpiresAt = now.AddHours(48);
            existing.UpdatedAt = now;

            uow.Newsletters.UpdateSubscriber(existing);
            await WriteConfirmationOutbox(existing.SubscriberId, normalizedEmail, rawToken, now, cancellationToken);

            try
            {
                await uow.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrent insert for same email — treat as success
                return;
            }

            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "subscriber.subscribed", new { SubscriberId = existing.SubscriberId });
            return;
        }

        // New subscriber
        var (newRawToken, newTokenHash) = GenerateConfirmationToken();
        var subscriber = new NewsletterSubscriber
        {
            SubscriberId = Guid.NewGuid(),
            Email = normalizedEmail,
            ConfirmationTokenHash = newTokenHash,
            TokenExpiresAt = now.AddHours(48),
            Confirmed = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await uow.Newsletters.AddSubscriberAsync(subscriber, cancellationToken);
        await WriteConfirmationOutbox(subscriber.SubscriberId, normalizedEmail, newRawToken, now, cancellationToken);

        try
        {
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation on email — treat as success
            return;
        }

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "subscriber.subscribed", new { subscriber.SubscriberId });
    }

    private async Task WriteConfirmationOutbox(Guid subscriberId, string email, string rawToken, DateTime now, CancellationToken cancellationToken)
    {
        var siteUrl = configuration["Site:SiteUrl"] ?? string.Empty;
        var confirmUrl = $"{siteUrl}/newsletter/confirm?token={Uri.EscapeDataString(rawToken)}";

        var payload = JsonSerializer.Serialize(new
        {
            subscriberId,
            email,
            confirmUrl
        });

        await uow.Newsletters.AddOutboxMessageAsync(new OutboxMessage
        {
            OutboxMessageId = Guid.NewGuid(),
            MessageType = "ConfirmationEmail",
            Payload = payload,
            CreatedAt = now,
            Status = OutboxMessageStatus.Pending
        }, cancellationToken);
    }

    private static (string RawToken, string TokenHash) GenerateConfirmationToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var hashBytes = SHA256.HashData(tokenBytes);
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return (rawToken, tokenHash);
    }
}
