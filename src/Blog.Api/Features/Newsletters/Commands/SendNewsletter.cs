using Blog.Api.Common.Exceptions;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace Blog.Api.Features.Newsletters.Commands;

public record SendNewsletterCommand(Guid Id) : IRequest;

public class SendNewsletterCommandHandler(
    IUnitOfWork uow,
    ISlugGenerator slugGenerator,
    ICacheInvalidator cacheInvalidator,
    ILogger<SendNewsletterCommandHandler> logger) : IRequestHandler<SendNewsletterCommand>
{
    private const int MaxSlugRetries = 10;

    public async Task Handle(SendNewsletterCommand request, CancellationToken cancellationToken)
    {
        var newsletter = await uow.Newsletters.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Newsletter with id '{request.Id}' not found.");

        if (newsletter.Status == NewsletterStatus.Sent)
            throw new ConflictException("Newsletter has already been sent.");

        var now = DateTime.UtcNow;

        // Generate slug
        var slug = GenerateUniqueSlug(newsletter, slugGenerator);
        slug = await EnsureUniqueSlugAsync(slug, newsletter.NewsletterId, cancellationToken);

        newsletter.Status = NewsletterStatus.Sent;
        newsletter.DateSent = now;
        newsletter.Slug = slug;
        newsletter.UpdatedAt = now;

        // Stream confirmed subscribers and create outbox messages
        var outboxMessages = new List<OutboxMessage>();
        await foreach (var (subscriberId, email) in uow.Newsletters.StreamConfirmedSubscribersAsync(cancellationToken))
        {
            var payload = JsonSerializer.Serialize(new
            {
                newsletterId = newsletter.NewsletterId,
                subscriberId,
                email
            });

            outboxMessages.Add(new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageType = "NewsletterDelivery",
                Payload = payload,
                CreatedAt = now,
                Status = OutboxMessageStatus.Pending
            });
        }

        if (outboxMessages.Count == 0)
        {
            logger.LogWarning("Business event {EventType} occurred: {@Details}",
                "newsletter.sent", new { newsletter.NewsletterId, RecipientCount = 0 });
        }
        else
        {
            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "newsletter.sent", new { newsletter.NewsletterId, RecipientCount = outboxMessages.Count });
        }

        uow.Newsletters.Update(newsletter);
        if (outboxMessages.Count > 0)
            await uow.Newsletters.AddOutboxMessagesAsync(outboxMessages, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateNewsletterArchive();
    }

    private static string GenerateUniqueSlug(Newsletter newsletter, ISlugGenerator slugGenerator)
    {
        var slug = slugGenerator.Generate(newsletter.Subject);
        if (string.IsNullOrWhiteSpace(slug))
            slug = newsletter.NewsletterId.ToString("N").ToLowerInvariant();
        return slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid newsletterId, CancellationToken cancellationToken)
    {
        if (!await uow.Newsletters.SlugExistsAsync(slug, cancellationToken))
            return slug;

        for (var i = 2; i <= MaxSlugRetries + 1; i++)
        {
            var candidate = $"{slug}-{i}";
            if (!await uow.Newsletters.SlugExistsAsync(candidate, cancellationToken))
                return candidate;
        }

        // Fall back to newsletter ID as slug (guaranteed unique)
        return newsletterId.ToString("N").ToLowerInvariant();
    }
}
