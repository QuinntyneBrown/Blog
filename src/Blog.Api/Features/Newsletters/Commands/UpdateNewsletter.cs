using Blog.Api.Common.Exceptions;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Newsletters.Commands;

public record UpdateNewsletterCommand(Guid Id, string Subject, string Body, int Version) : IRequest<NewsletterDto>;

public class UpdateNewsletterCommandValidator : AbstractValidator<UpdateNewsletterCommand>
{
    public UpdateNewsletterCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Version).GreaterThan(0);
    }
}

public class UpdateNewsletterCommandHandler(
    IUnitOfWork uow,
    IMarkdownConverter markdownConverter) : IRequestHandler<UpdateNewsletterCommand, NewsletterDto>
{
    public async Task<NewsletterDto> Handle(UpdateNewsletterCommand request, CancellationToken cancellationToken)
    {
        var newsletter = await uow.Newsletters.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Newsletter with id '{request.Id}' not found.");

        if (newsletter.Status == NewsletterStatus.Sent)
            throw new ConflictException("Cannot update a newsletter that has already been sent.");

        if (newsletter.Version != request.Version)
            throw new ConflictException($"Version mismatch. Expected {request.Version}, but found {newsletter.Version}.");

        newsletter.Subject = request.Subject;
        newsletter.Body = request.Body;
        newsletter.BodyHtml = markdownConverter.Convert(request.Body);
        newsletter.UpdatedAt = DateTime.UtcNow;

        uow.Newsletters.Update(newsletter);
        await uow.SaveChangesAsync(cancellationToken);

        return new NewsletterDto(
            newsletter.NewsletterId, newsletter.Subject, newsletter.Slug,
            newsletter.Body, newsletter.BodyHtml,
            newsletter.Status.ToString(), newsletter.DateSent,
            newsletter.CreatedAt, newsletter.UpdatedAt, newsletter.Version);
    }
}
