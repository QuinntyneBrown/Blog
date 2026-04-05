using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;

using MediatR;

namespace Blog.Api.Features.Articles.Queries;

public record GetPublishedArticleBySlugQuery(string Slug) : IRequest<ArticleDto>;

public class GetPublishedArticleBySlugHandler(IArticleRepository articles) : IRequestHandler<GetPublishedArticleBySlugQuery, ArticleDto>
{
    public async Task<ArticleDto> Handle(GetPublishedArticleBySlugQuery request, CancellationToken cancellationToken)
    {
        var article = await articles.GetBySlugAsync(request.Slug, cancellationToken);

        if (article == null || !article.Published)
            throw new NotFoundException($"Article with slug '{request.Slug}' was not found.");

        return new ArticleDto(
            article.ArticleId, article.Title, article.Slug, article.Abstract,
            article.Body, article.BodyHtml, article.FeaturedImageId,
            article.FeaturedImage != null ? $"/assets/{article.FeaturedImage.StoredFileName}" : null,
            article.FeaturedImage?.Width > 0 ? article.FeaturedImage.Width : (int?)null,
            article.FeaturedImage?.Height > 0 ? article.FeaturedImage.Height : (int?)null,
            article.Published, article.DatePublished,
            article.ReadingTimeMinutes, article.CreatedAt, article.UpdatedAt, article.Version);
    }
}
