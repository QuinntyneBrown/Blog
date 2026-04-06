using Blog.Api.Common.Exceptions;
using Blog.Api.Common.Models;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Newsletters.Queries;

public record GetNewslettersQuery(int Page = 1, int PageSize = 20, string? Status = null) : IRequest<PagedResponse<NewsletterListDto>>;

public class GetNewslettersHandler(INewsletterRepository newsletters) : IRequestHandler<GetNewslettersQuery, PagedResponse<NewsletterListDto>>
{
    public async Task<PagedResponse<NewsletterListDto>> Handle(GetNewslettersQuery request, CancellationToken cancellationToken)
    {
        NewsletterStatus? status = null;
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (!Enum.TryParse<NewsletterStatus>(request.Status, ignoreCase: true, out var parsed))
                throw new BadRequestException($"Invalid status value '{request.Status}'. Valid values are: Draft, Sent.");
            status = parsed;
        }

        var items = await newsletters.GetAllAsync(request.Page, request.PageSize, status, cancellationToken);
        var total = await newsletters.GetAllCountAsync(status, cancellationToken);

        return new PagedResponse<NewsletterListDto>
        {
            Items = items.Select(n => new NewsletterListDto(
                n.NewsletterId, n.Subject, n.Slug, n.Status.ToString(), n.DateSent, n.CreatedAt)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
