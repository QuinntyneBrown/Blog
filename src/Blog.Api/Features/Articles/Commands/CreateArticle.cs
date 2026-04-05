using Blog.Api.Common.Exceptions;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data;
using Blog.Api.Services;
using FluentValidation;
using MediatR;
using Blog.Api.Features.Articles.Queries;

namespace Blog.Api.Features.Articles.Commands;

public record CreateArticleCommand(string Title, string Body, string Abstract, Guid? FeaturedImageId) : IRequest<ArticleDto>;

public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Abstract).NotEmpty().MaximumLength(512);
    }
}

public class CreateArticleCommandHandler(
    IUnitOfWork uow,
    ISlugGenerator slugGenerator,
    IMarkdownConverter markdownConverter,
    IReadingTimeCalculator readingTimeCalculator) : IRequestHandler<CreateArticleCommand, ArticleDto>
{
    public async Task<ArticleDto> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        var slug = slugGenerator.Generate(request.Title);

        if (await uow.Articles.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            throw new ConflictException($"An article with slug '{slug}' already exists.");

        var bodyHtml = markdownConverter.Convert(request.Body);
        var readingTime = readingTimeCalculator.Calculate(request.Body);

        var article = new Article
        {
            ArticleId = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Abstract = request.Abstract,
            Body = request.Body,
            BodyHtml = bodyHtml,
            FeaturedImageId = request.FeaturedImageId,
            Published = false,
            ReadingTimeMinutes = readingTime,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await uow.Articles.AddAsync(article, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return new ArticleDto(
            article.ArticleId, article.Title, article.Slug, article.Abstract,
            article.Body, article.BodyHtml, article.FeaturedImageId, null,
            null, null,
            article.Published, article.DatePublished,
            article.ReadingTimeMinutes, article.CreatedAt, article.UpdatedAt, article.Version);
    }
}
