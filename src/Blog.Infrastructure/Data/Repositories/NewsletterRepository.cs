using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Blog.Infrastructure.Data.Repositories;

public class NewsletterRepository(BlogDbContext context) : INewsletterRepository
{
    // Newsletter methods

    public async Task<Newsletter?> GetByIdAsync(Guid newsletterId, CancellationToken cancellationToken = default)
        => await context.Newsletters.FirstOrDefaultAsync(n => n.NewsletterId == newsletterId, cancellationToken);

    public async Task<IReadOnlyList<Newsletter>> GetAllAsync(int page, int pageSize, NewsletterStatus? status, CancellationToken cancellationToken = default)
    {
        var query = context.Newsletters.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        query = status == NewsletterStatus.Sent
            ? query.OrderByDescending(n => n.DateSent)
            : query.OrderByDescending(n => n.CreatedAt);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new Newsletter
            {
                NewsletterId = n.NewsletterId,
                Subject = n.Subject,
                Slug = n.Slug,
                Status = n.Status,
                DateSent = n.DateSent,
                CreatedAt = n.CreatedAt,
                Body = string.Empty,
                BodyHtml = string.Empty
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAllCountAsync(NewsletterStatus? status, CancellationToken cancellationToken = default)
    {
        var query = context.Newsletters.AsQueryable();
        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Newsletters
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Slug == slug && n.Status == NewsletterStatus.Sent, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Newsletters.AnyAsync(n => n.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Newsletter>> GetSentAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Newsletters
            .AsNoTracking()
            .Where(n => n.Status == NewsletterStatus.Sent)
            .OrderByDescending(n => n.DateSent)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new Newsletter
            {
                NewsletterId = n.NewsletterId,
                Subject = n.Subject,
                Slug = n.Slug,
                Status = n.Status,
                DateSent = n.DateSent,
                CreatedAt = n.CreatedAt,
                Body = string.Empty,
                BodyHtml = string.Empty
            })
            .ToListAsync(cancellationToken);

    public async Task<int> GetSentCountAsync(CancellationToken cancellationToken = default)
        => await context.Newsletters.CountAsync(n => n.Status == NewsletterStatus.Sent, cancellationToken);

    public async Task<DateTime?> GetLatestDateSentAsync(CancellationToken cancellationToken = default)
        => await context.Newsletters
            .Where(n => n.Status == NewsletterStatus.Sent)
            .MaxAsync(n => (DateTime?)n.DateSent, cancellationToken);

    public async Task AddAsync(Newsletter newsletter, CancellationToken cancellationToken = default)
        => await context.Newsletters.AddAsync(newsletter, cancellationToken);

    public void Update(Newsletter newsletter) => context.Newsletters.Update(newsletter);
    public void Remove(Newsletter newsletter) => context.Newsletters.Remove(newsletter);

    // Subscriber methods

    public async Task<NewsletterSubscriber?> GetSubscriberByIdAsync(Guid subscriberId, CancellationToken cancellationToken = default)
        => await context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.SubscriberId == subscriberId, cancellationToken);

    public async Task<NewsletterSubscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

    public async Task<NewsletterSubscriber?> GetSubscriberByConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => await context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.ConfirmationTokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<NewsletterSubscriber>> GetSubscribersAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        var query = ApplySubscriberStatusFilter(context.NewsletterSubscribers.AsNoTracking(), status);
        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetSubscribersCountAsync(string? status, CancellationToken cancellationToken = default)
    {
        var query = ApplySubscriberStatusFilter(context.NewsletterSubscribers.AsQueryable(), status);
        return await query.CountAsync(cancellationToken);
    }

    public async IAsyncEnumerable<(Guid SubscriberId, string Email)> StreamConfirmedSubscribersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in context.NewsletterSubscribers
            .AsNoTracking()
            .Where(s => s.Confirmed && s.IsActive)
            .Select(s => new { s.SubscriberId, s.Email })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return (item.SubscriberId, item.Email);
        }
    }

    public async Task AddSubscriberAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken = default)
        => await context.NewsletterSubscribers.AddAsync(subscriber, cancellationToken);

    public void UpdateSubscriber(NewsletterSubscriber subscriber) => context.NewsletterSubscribers.Update(subscriber);

    // SendLog methods

    public async Task<bool> SendLogExistsAsync(Guid newsletterId, string recipientIdempotencyKey, CancellationToken cancellationToken = default)
        => await context.NewsletterSendLogs.AnyAsync(
            l => l.NewsletterId == newsletterId && l.RecipientIdempotencyKey == recipientIdempotencyKey,
            cancellationToken);

    public async Task AddSendLogAsync(NewsletterSendLog sendLog, CancellationToken cancellationToken = default)
        => await context.NewsletterSendLogs.AddAsync(sendLog, cancellationToken);

    // Outbox methods

    public async Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        => await context.OutboxMessages.AddAsync(message, cancellationToken);

    public async Task AddOutboxMessagesAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
        => await context.OutboxMessages.AddRangeAsync(messages, cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await context.OutboxMessages
            .Where(o => o.Status == OutboxMessageStatus.Pending && (o.NextRetryAt == null || o.NextRetryAt <= now))
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<OutboxMessage?> GetOutboxMessageByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.OutboxMessages.FirstOrDefaultAsync(o => o.OutboxMessageId == id, cancellationToken);

    public async Task<int> ReplayDeadLetteredMessagesAsync(string? messageType, CancellationToken cancellationToken = default)
    {
        var query = context.OutboxMessages.Where(o => o.Status == OutboxMessageStatus.DeadLettered);
        if (!string.IsNullOrEmpty(messageType))
            query = query.Where(o => o.MessageType == messageType);

        var messages = await query.ToListAsync(cancellationToken);
        foreach (var msg in messages)
        {
            msg.Status = OutboxMessageStatus.Pending;
            msg.RetryCount = 0;
            msg.NextRetryAt = null;
            msg.ProcessedAt = null;
            msg.Error = null;
        }
        return messages.Count;
    }

    public void UpdateOutboxMessage(OutboxMessage message) => context.OutboxMessages.Update(message);

    private static IQueryable<NewsletterSubscriber> ApplySubscriberStatusFilter(IQueryable<NewsletterSubscriber> query, string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "confirmed" => query.Where(s => s.Confirmed && s.IsActive),
            "unconfirmed" => query.Where(s => !s.Confirmed && s.IsActive),
            "inactive" => query.Where(s => !s.IsActive),
            _ => query
        };
    }
}
