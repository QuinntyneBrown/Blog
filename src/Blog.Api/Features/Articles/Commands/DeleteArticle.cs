using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;
using Blog.Api.Services;
using Blog.Infrastructure.Data;
using MediatR;

namespace Blog.Api.Features.Articles.Commands;

public record DeleteArticleCommand(Guid Id, string? IfMatch) : IRequest;

public class DeleteArticleCommandHandler(IUnitOfWork uow, ICacheInvalidator cacheInvalidator) : IRequestHandler<DeleteArticleCommand>
{
    public async Task Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await uow.Articles.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Article with ID '{request.Id}' was not found.");

        var expectedETag = $"W/\"article-{article.ArticleId}-v{article.Version}\"";
        if (!string.IsNullOrEmpty(request.IfMatch) && request.IfMatch != expectedETag)
            throw new PreconditionFailedException("The article has been modified. Please refresh and try again.");

        var slug = article.Slug;
        article.FeaturedImageId = null;
        uow.Articles.Remove(article);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateArticle(slug);
    }
}
