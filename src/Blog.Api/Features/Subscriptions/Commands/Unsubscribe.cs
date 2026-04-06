using Blog.Api.Services;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Subscriptions.Commands;

public record UnsubscribeCommand(string Token) : IRequest;

public class UnsubscribeCommandHandler(
    IUnitOfWork uow,
    IUnsubscribeTokenService tokenService,
    ILogger<UnsubscribeCommandHandler> logger) : IRequestHandler<UnsubscribeCommand>
{
    public async Task Handle(UnsubscribeCommand request, CancellationToken cancellationToken)
    {
        var subscriberId = tokenService.ValidateAndExtractSubscriberId(request.Token);
        if (subscriberId == null)
            return; // Invalid HMAC — silent no-op (prevents enumeration)

        var subscriber = await uow.Newsletters.GetSubscriberByIdAsync(subscriberId.Value, cancellationToken);
        if (subscriber == null)
            return; // Subscriber not found — silent no-op

        if (!subscriber.IsActive)
            return; // Already unsubscribed

        subscriber.IsActive = false;
        subscriber.ConfirmationTokenHash = null;
        subscriber.TokenExpiresAt = null;
        subscriber.UpdatedAt = DateTime.UtcNow;

        uow.Newsletters.UpdateSubscriber(subscriber);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "subscriber.unsubscribed", new { subscriber.SubscriberId });
    }
}
