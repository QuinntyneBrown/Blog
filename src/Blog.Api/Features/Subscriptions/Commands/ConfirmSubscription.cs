using Blog.Api.Common.Exceptions;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Security.Cryptography;

namespace Blog.Api.Features.Subscriptions.Commands;

public record ConfirmSubscriptionCommand(string Token) : IRequest;

public class ConfirmSubscriptionCommandValidator : AbstractValidator<ConfirmSubscriptionCommand>
{
    public ConfirmSubscriptionCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);
    }
}

public class ConfirmSubscriptionCommandHandler(
    IUnitOfWork uow,
    ILogger<ConfirmSubscriptionCommandHandler> logger) : IRequestHandler<ConfirmSubscriptionCommand>
{
    public async Task Handle(ConfirmSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Compute SHA-256 hash of the raw token
        var tokenBytes = Convert.FromHexString(request.Token);
        var hashBytes = SHA256.HashData(tokenBytes);
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var subscriber = await uow.Newsletters.GetSubscriberByConfirmationTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnprocessableEntityException("Confirmation token is invalid or has already been used.");

        // Check expiry — null TokenExpiresAt is treated as expired (defensive guard)
        var now = DateTime.UtcNow;
        if (subscriber.TokenExpiresAt == null || subscriber.TokenExpiresAt < now)
            throw new UnprocessableEntityException("Confirmation token has expired.");

        subscriber.Confirmed = true;
        subscriber.ConfirmedAt = now;
        subscriber.ConfirmationTokenHash = null;
        subscriber.TokenExpiresAt = null;
        subscriber.UpdatedAt = now;

        uow.Newsletters.UpdateSubscriber(subscriber);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "subscriber.confirmed", new { subscriber.SubscriberId });
    }
}
