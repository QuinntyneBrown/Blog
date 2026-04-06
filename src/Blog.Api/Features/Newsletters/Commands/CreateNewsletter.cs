using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Newsletters.Commands;

public record CreateNewsletterCommand(string Subject, string Body) : IRequest<NewsletterDto>;

public class CreateNewsletterCommandValidator : AbstractValidator<CreateNewsletterCommand>
{
    public CreateNewsletterCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class CreateNewsletterCommandHandler(
    IUnitOfWork uow,
    IMarkdownConverter markdownConverter) : IRequestHandler<CreateNewsletterCommand, NewsletterDto>
{
    public async Task<NewsletterDto> Handle(CreateNewsletterCommand request, CancellationToken cancellationToken)
    {
        var bodyHtml = markdownConverter.Convert(request.Body);
        var now = DateTime.UtcNow;

        var newsletter = new Newsletter
        {
            NewsletterId = Guid.NewGuid(),
            Subject = request.Subject,
            Body = request.Body,
            BodyHtml = bodyHtml,
            Status = NewsletterStatus.Draft,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        await uow.Newsletters.AddAsync(newsletter, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return new NewsletterDto(
            newsletter.NewsletterId, newsletter.Subject, newsletter.Slug,
            newsletter.Body, newsletter.BodyHtml,
            newsletter.Status.ToString(), newsletter.DateSent,
            newsletter.CreatedAt, newsletter.UpdatedAt, newsletter.Version);
    }
}
