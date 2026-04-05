using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;

using MediatR;

namespace Blog.Api.Features.Articles.Queries;

public record ArticleDto(
    Guid ArticleId, string Title, string Slug, string Abstract,
    string Body, string BodyHtml, Guid? FeaturedImageId, string? FeaturedImageUrl,
    bool Published, DateTime? DatePublished,
    int ReadingTimeMinutes, DateTime CreatedAt, DateTime UpdatedAt, int Version);

public record GetArticleByIdQuery(Guid Id) : IRequest<ArticleDto>;

public class GetArticleByIdHandler(IArticleRepository articles) : IRequestHandler<GetArticleByIdQuery, ArticleDto>
{
    public async Task<ArticleDto> Handle(GetArticleByIdQuery request, CancellationToken cancellationToken)
    {
        var article = await articles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Article with ID '{request.Id}' was not found.");

        return new ArticleDto(
            article.ArticleId, article.Title, article.Slug, article.Abstract,
            article.Body, article.BodyHtml, article.FeaturedImageId,
            article.FeaturedImage != null ? $"/assets/{article.FeaturedImage.StoredFileName}" : null,
            article.Published, article.DatePublished,
            article.ReadingTimeMinutes, article.CreatedAt, article.UpdatedAt, article.Version);
    }
}
