using Blog.Domain.Entities;

namespace Blog.Domain.Interfaces;

public interface INewsletterRepository
{
    // Newsletter methods
    Task<Newsletter?> GetByIdAsync(Guid newsletterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Newsletter>> GetAllAsync(int page, int pageSize, NewsletterStatus? status, CancellationToken cancellationToken = default);
    Task<int> GetAllCountAsync(NewsletterStatus? status, CancellationToken cancellationToken = default);
    Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Newsletter>> GetSentAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetSentCountAsync(CancellationToken cancellationToken = default);
    Task<DateTime?> GetLatestDateSentAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Newsletter newsletter, CancellationToken cancellationToken = default);
    void Update(Newsletter newsletter);
    void Remove(Newsletter newsletter);

    // Subscriber methods
    Task<NewsletterSubscriber?> GetSubscriberByIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);
    Task<NewsletterSubscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<NewsletterSubscriber?> GetSubscriberByConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NewsletterSubscriber>> GetSubscribersAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<int> GetSubscribersCountAsync(string? status, CancellationToken cancellationToken = default);
    IAsyncEnumerable<(Guid SubscriberId, string Email)> StreamConfirmedSubscribersAsync(CancellationToken cancellationToken = default);
    Task AddSubscriberAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken = default);
    void UpdateSubscriber(NewsletterSubscriber subscriber);

    // SendLog methods
    Task<bool> SendLogExistsAsync(Guid newsletterId, string recipientIdempotencyKey, CancellationToken cancellationToken = default);
    Task AddSendLogAsync(NewsletterSendLog sendLog, CancellationToken cancellationToken = default);

    // Outbox methods
    Task AddOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task AddOutboxMessagesAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> GetOutboxMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> ReplayDeadLetteredMessagesAsync(string? messageType, CancellationToken cancellationToken = default);
    void UpdateOutboxMessage(OutboxMessage message);
}
