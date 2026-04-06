using Blog.Api.Common.Exceptions;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Outbox.Commands;

public record ReplayOutboxMessageCommand(Guid Id) : IRequest;

public class ReplayOutboxMessageCommandHandler(IUnitOfWork uow) : IRequestHandler<ReplayOutboxMessageCommand>
{
    public async Task Handle(ReplayOutboxMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await uow.Newsletters.GetOutboxMessageByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Outbox message with id '{request.Id}' not found.");

        if (message.Status != OutboxMessageStatus.DeadLettered)
            throw new ConflictException("Only dead-lettered messages can be replayed.");

        message.Status = OutboxMessageStatus.Pending;
        message.RetryCount = 0;
        message.NextRetryAt = null;
        message.ProcessedAt = null;
        message.Error = null;

        uow.Newsletters.UpdateOutboxMessage(message);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
