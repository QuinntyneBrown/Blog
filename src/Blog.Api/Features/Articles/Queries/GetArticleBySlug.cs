using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;

using MediatR;

namespace Blog.Api.Features.Articles.Queries;

public record GetArticleBySlugQuery(string Slug) : IRequest<ArticleDto>;

public class GetArticleBySlugHandler(IArticleRepository articles) : IRequestHandler<GetArticleBySlugQuery, ArticleDto>
{
    public async Task<ArticleDto> Handle(GetArticleBySlugQuery request, CancellationToken cancellationToken)
    {
        var article = await articles.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException($"Article with slug '{request.Slug}' was not found.");

        return new ArticleDto(
            article.ArticleId, article.Title, article.Slug, article.Abstract,
            article.Body, article.BodyHtml, article.FeaturedImageId,
            article.FeaturedImage != null ? $"/assets/{article.FeaturedImage.StoredFileName}" : null,
            article.Published, article.DatePublished,
            article.ReadingTimeMinutes, article.CreatedAt, article.UpdatedAt, article.Version);
    }
}
