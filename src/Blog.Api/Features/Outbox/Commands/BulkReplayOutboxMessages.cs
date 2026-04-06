using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Outbox.Commands;

public record BulkReplayOutboxMessagesCommand(string? MessageType = null) : IRequest<int>;

public class BulkReplayOutboxMessagesCommandHandler(IUnitOfWork uow) : IRequestHandler<BulkReplayOutboxMessagesCommand, int>
{
    public async Task<int> Handle(BulkReplayOutboxMessagesCommand request, CancellationToken cancellationToken)
    {
        var count = await uow.Newsletters.ReplayDeadLetteredMessagesAsync(request.MessageType, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return count;
    }
}
