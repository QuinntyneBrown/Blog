using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Events.Queries;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Events.Commands;

public record UnpublishEventCommand(Guid EventId, int Version) : IRequest<EventDto>;

public class UnpublishEventCommandValidator : AbstractValidator<UnpublishEventCommand>
{
    public UnpublishEventCommandValidator()
    {
        RuleFor(x => x.Version).GreaterThan(0);
    }
}

public class UnpublishEventCommandHandler(
    IUnitOfWork uow,
    ICacheInvalidator cacheInvalidator,
    ILogger<UnpublishEventCommandHandler> logger) : IRequestHandler<UnpublishEventCommand, EventDto>
{
    public async Task<EventDto> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await uow.Events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{request.EventId}' was not found.");

        if (request.Version != ev.Version)
            throw new ConflictException("The event has been modified by another request. Please re-fetch and try again.");

        if (!ev.Published)
            return EventDto.FromEntity(ev);

        ev.Published = false;
        ev.UpdatedAt = DateTime.UtcNow;

        uow.Events.Update(ev);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateEvent(ev.Slug);

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "event.unpublished", new { ev.EventId });

        return EventDto.FromEntity(ev);
    }
}
