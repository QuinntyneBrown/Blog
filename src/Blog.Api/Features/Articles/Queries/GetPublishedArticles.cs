using Blog.Domain.Interfaces;
using Blog.Api.Common.Models;

using MediatR;

namespace Blog.Api.Features.Articles.Queries;

public record GetPublishedArticlesQuery(int Page = 1, int PageSize = 9) : IRequest<PagedResponse<ArticleListDto>>;

public class GetPublishedArticlesHandler(IArticleRepository articles) : IRequestHandler<GetPublishedArticlesQuery, PagedResponse<ArticleListDto>>
{
    public async Task<PagedResponse<ArticleListDto>> Handle(GetPublishedArticlesQuery request, CancellationToken cancellationToken)
    {
        var items = await articles.GetPublishedAsync(request.Page, request.PageSize, cancellationToken);
        var total = await articles.GetPublishedCountAsync(cancellationToken);
        return new PagedResponse<ArticleListDto>
        {
            Items = items.Select(a => new ArticleListDto(
                a.ArticleId, a.Title, a.Slug, a.Abstract,
                a.FeaturedImageId, a.FeaturedImage != null ? $"/assets/{a.FeaturedImage.StoredFileName}" : null,
                a.FeaturedImage?.Width > 0 ? a.FeaturedImage.Width : (int?)null,
                a.FeaturedImage?.Height > 0 ? a.FeaturedImage.Height : (int?)null,
                a.Published, a.DatePublished,
                a.ReadingTimeMinutes, a.CreatedAt, a.UpdatedAt, a.Version)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
