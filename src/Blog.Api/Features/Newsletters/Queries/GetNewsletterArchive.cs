using Blog.Api.Common.Models;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Newsletters.Queries;

public record GetNewsletterArchiveQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResponse<NewsletterArchiveDto>>;

public class GetNewsletterArchiveHandler(INewsletterRepository newsletters) : IRequestHandler<GetNewsletterArchiveQuery, PagedResponse<NewsletterArchiveDto>>
{
    public async Task<PagedResponse<NewsletterArchiveDto>> Handle(GetNewsletterArchiveQuery request, CancellationToken cancellationToken)
    {
        var items = await newsletters.GetSentAsync(request.Page, request.PageSize, cancellationToken);
        var total = await newsletters.GetSentCountAsync(cancellationToken);

        return new PagedResponse<NewsletterArchiveDto>
        {
            Items = items.Select(n => new NewsletterArchiveDto(n.Subject, n.Slug!, n.DateSent)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
