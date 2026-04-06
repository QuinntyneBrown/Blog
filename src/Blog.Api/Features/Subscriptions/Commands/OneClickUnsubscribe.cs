using Blog.Api.Services;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Subscriptions.Commands;

public record OneClickUnsubscribeCommand(string Token, string Body) : IRequest;

// No FluentValidation validator — RFC 8058 requires this endpoint to always return 200,
// even on malformed input. Validation is performed inline in the handler as a silent no-op.

public class OneClickUnsubscribeCommandHandler(
    IUnitOfWork uow,
    IUnsubscribeTokenService tokenService,
    ILogger<OneClickUnsubscribeCommandHandler> logger) : IRequestHandler<OneClickUnsubscribeCommand>
{
    public async Task Handle(OneClickUnsubscribeCommand request, CancellationToken cancellationToken)
    {
        // Validate body matches RFC 8058 requirement
        if (string.IsNullOrEmpty(request.Token) || request.Body != "List-Unsubscribe=One-Click")
            return; // Silent no-op per RFC 8058

        var subscriberId = tokenService.ValidateAndExtractSubscriberId(request.Token);
        if (subscriberId == null)
            return; // Invalid HMAC — silent no-op

        var subscriber = await uow.Newsletters.GetSubscriberByIdAsync(subscriberId.Value, cancellationToken);
        if (subscriber == null || !subscriber.IsActive)
            return;

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
