using Blog.Domain.Interfaces;
using Blog.Api.Common.Models;

using MediatR;

namespace Blog.Api.Features.Articles.Queries;

public record ArticleListDto(
    Guid ArticleId, string Title, string Slug, string Abstract,
    Guid? FeaturedImageId, string? FeaturedImageUrl, int? FeaturedImageWidth, int? FeaturedImageHeight,
    bool Published, DateTime? DatePublished,
    int ReadingTimeMinutes, DateTime CreatedAt, DateTime UpdatedAt, int Version);

public record GetArticlesQuery(int Page = 1, int PageSize = 9) : IRequest<PagedResponse<ArticleListDto>>;

public class GetArticlesHandler(IArticleRepository articles) : IRequestHandler<GetArticlesQuery, PagedResponse<ArticleListDto>>
{
    public async Task<PagedResponse<ArticleListDto>> Handle(GetArticlesQuery request, CancellationToken cancellationToken)
    {
        var items = await articles.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        var total = await articles.GetAllCountAsync(cancellationToken);
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
