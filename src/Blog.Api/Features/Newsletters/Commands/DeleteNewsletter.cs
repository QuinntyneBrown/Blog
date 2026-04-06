using Blog.Api.Common.Exceptions;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Newsletters.Commands;

public record DeleteNewsletterCommand(Guid Id) : IRequest;

public class DeleteNewsletterCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteNewsletterCommand>
{
    public async Task Handle(DeleteNewsletterCommand request, CancellationToken cancellationToken)
    {
        var newsletter = await uow.Newsletters.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Newsletter with id '{request.Id}' not found.");

        if (newsletter.Status == NewsletterStatus.Sent)
            throw new ConflictException("Cannot delete a newsletter that has already been sent.");

        uow.Newsletters.Remove(newsletter);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
