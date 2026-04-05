using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;
using Blog.Api.Services;
using Blog.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Blog.Api.Features.Articles.Queries;

namespace Blog.Api.Features.Articles.Commands;

public record UpdateArticleCommand(Guid Id, string Title, string Body, string Abstract, Guid? FeaturedImageId, string? IfMatch) : IRequest<ArticleDto>;

public class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Abstract).NotEmpty().MaximumLength(512);
    }
}

public class UpdateArticleCommandHandler(
    IUnitOfWork uow,
    ISlugGenerator slugGenerator,
    IMarkdownConverter markdownConverter,
    IReadingTimeCalculator readingTimeCalculator,
    ICacheInvalidator cacheInvalidator) : IRequestHandler<UpdateArticleCommand, ArticleDto>
{
    public async Task<ArticleDto> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await uow.Articles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Article with ID '{request.Id}' was not found.");

        var expectedETag = $"W/\"article-{article.ArticleId}-v{article.Version}\"";
        if (!string.IsNullOrEmpty(request.IfMatch) && request.IfMatch != expectedETag)
            throw new PreconditionFailedException("The article has been modified. Please refresh and try again.");

        if (article.Title != request.Title && !article.Published)
        {
            var slug = slugGenerator.Generate(request.Title);
            if (await uow.Articles.SlugExistsAsync(slug, article.ArticleId, cancellationToken))
                throw new ConflictException($"An article with slug '{slug}' already exists.");
            article.Slug = slug;
        }

        if (article.Body != request.Body)
        {
            article.BodyHtml = markdownConverter.Convert(request.Body);
            article.ReadingTimeMinutes = readingTimeCalculator.Calculate(request.Body);
        }

        article.Title = request.Title;
        article.Body = request.Body;
        article.Abstract = request.Abstract;
        article.FeaturedImageId = request.FeaturedImageId;
        article.UpdatedAt = DateTime.UtcNow;

        uow.Articles.Update(article);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateArticle(article.Slug);

        return new ArticleDto(
            article.ArticleId, article.Title, article.Slug, article.Abstract,
            article.Body, article.BodyHtml, article.FeaturedImageId,
            article.Published, article.DatePublished,
            article.ReadingTimeMinutes, article.CreatedAt, article.UpdatedAt, article.Version);
    }
}
