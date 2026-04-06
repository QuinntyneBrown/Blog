using Blog.Api.Common.Exceptions;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Newsletters.Queries;

public record GetNewsletterByIdQuery(Guid NewsletterId) : IRequest<NewsletterDto>;

public class GetNewsletterByIdHandler(INewsletterRepository newsletters) : IRequestHandler<GetNewsletterByIdQuery, NewsletterDto>
{
    public async Task<NewsletterDto> Handle(GetNewsletterByIdQuery request, CancellationToken cancellationToken)
    {
        var newsletter = await newsletters.GetByIdAsync(request.NewsletterId, cancellationToken)
            ?? throw new NotFoundException($"Newsletter with id '{request.NewsletterId}' not found.");

        return new NewsletterDto(
            newsletter.NewsletterId, newsletter.Subject, newsletter.Slug,
            newsletter.Body, newsletter.BodyHtml,
            newsletter.Status.ToString(), newsletter.DateSent,
            newsletter.CreatedAt, newsletter.UpdatedAt, newsletter.Version);
    }
}
