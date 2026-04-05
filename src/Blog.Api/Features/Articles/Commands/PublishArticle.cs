using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;
using Blog.Api.Services;
using Blog.Infrastructure.Data;
using MediatR;
using Blog.Api.Features.Articles.Queries;

namespace Blog.Api.Features.Articles.Commands;

public record PublishArticleCommand(Guid Id, bool Published, string? IfMatch) : IRequest<ArticleDto>;

public class PublishArticleCommandHandler(
    IUnitOfWork uow,
    ILogger<PublishArticleCommandHandler> logger,
    ICacheInvalidator cacheInvalidator) : IRequestHandler<PublishArticleCommand, ArticleDto>
{
    public async Task<ArticleDto> Handle(PublishArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await uow.Articles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Article with ID '{request.Id}' was not found.");

        var expectedETag = $"W/\"article-{article.ArticleId}-v{article.Version}\"";
        if (!string.IsNullOrEmpty(request.IfMatch) && request.IfMatch != expectedETag)
            throw new PreconditionFailedException("The article has been modified. Please refresh and try again.");

        article.Published = request.Published;
        if (request.Published && article.DatePublished == null)
            article.DatePublished = DateTime.UtcNow;

        article.UpdatedAt = DateTime.UtcNow;
        uow.Articles.Update(article);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateArticle(article.Slug);

        if (request.Published)
            logger.LogInformation("Business event {EventType} occurred: {@Details}",
                "ArticlePublished", new { ArticleId = article.ArticleId, Slug = article.Slug });

        return new ArticleDto(
            article.ArticleId, article.Title, article.Slug, article.Abstract,
            article.Body, article.BodyHtml, article.FeaturedImageId,
            article.Published, article.DatePublished,
            article.ReadingTimeMinutes, article.CreatedAt, article.UpdatedAt, article.Version);
    }
}
