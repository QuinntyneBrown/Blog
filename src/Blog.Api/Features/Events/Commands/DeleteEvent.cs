using Blog.Api.Common.Exceptions;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Events.Commands;

public record DeleteEventCommand(Guid EventId) : IRequest;

public class DeleteEventCommandHandler(
    IUnitOfWork uow,
    ILogger<DeleteEventCommandHandler> logger) : IRequestHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await uow.Events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{request.EventId}' was not found.");

        if (ev.Published)
            throw new ConflictException("Cannot delete a published event. Unpublish the event first.");

        uow.Events.Remove(ev);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Business event {EventType} occurred: {@Details}",
            "event.deleted", new { ev.EventId });
    }
}
