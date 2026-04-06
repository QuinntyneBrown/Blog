using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Events.Queries;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Events.Commands;

public record PublishEventCommand(Guid EventId, int Version) : IRequest<EventDto>;

public class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventCommandValidator()
    {
        RuleFor(x => x.Version).GreaterThan(0);
    }
}

public class PublishEventCommandHandler(
    IUnitOfWork uow,
    ICacheInvalidator cacheInvalidator,
    ILogger<PublishEventCommandHandler> logger) : IRequestHandler<PublishEventCommand, EventDto>
{
    public async Task<EventDto> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await uow.Events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{request.EventId}' was not found.");

        if (request.Version != ev.Version)
            throw new ConflictException("The event has been modified by another request. Please re-fetch and try again.");

        if (ev.Published)
            return EventDto.FromEntity(ev);

        ev.Published = true;
        ev.FirstPublishedAt ??= DateTime.UtcNow;
        ev.UpdatedAt = DateTime.UtcNow;

        uow.Events.Update(ev);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateEvent(ev.Slug);

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "event.published", new { ev.EventId });

        return EventDto.FromEntity(ev);
    }
}
