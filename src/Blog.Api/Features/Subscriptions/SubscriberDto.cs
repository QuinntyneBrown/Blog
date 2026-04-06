namespace Blog.Api.Features.Subscriptions;

public record SubscriberDto(
    Guid SubscriberId, string Email, bool Confirmed, bool IsActive,
    DateTime? ConfirmedAt, DateTime? ResubscribedAt, DateTime CreatedAt, DateTime UpdatedAt);
