using Blog.Api.Common.Exceptions;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Newsletters.Queries;

public record GetNewsletterBySlugQuery(string Slug) : IRequest<NewsletterArchiveDetailDto>;

public class GetNewsletterBySlugHandler(INewsletterRepository newsletters) : IRequestHandler<GetNewsletterBySlugQuery, NewsletterArchiveDetailDto>
{
    public async Task<NewsletterArchiveDetailDto> Handle(GetNewsletterBySlugQuery request, CancellationToken cancellationToken)
    {
        var newsletter = await newsletters.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException($"Newsletter with slug '{request.Slug}' not found.");

        return new NewsletterArchiveDetailDto(
            newsletter.Subject, newsletter.Slug!, newsletter.BodyHtml, newsletter.DateSent);
    }
}
