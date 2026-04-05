using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Data.Repositories;

public class ArticleRepository(BlogDbContext context) : IArticleRepository
{
    public async Task<Article?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken = default)
        => await context.Articles.Include(a => a.FeaturedImage).FirstOrDefaultAsync(a => a.ArticleId == articleId, cancellationToken);

    public async Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Articles.Include(a => a.FeaturedImage).FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Article>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Articles
            .Include(a => a.FeaturedImage)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> GetAllCountAsync(CancellationToken cancellationToken = default)
        => await context.Articles.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Articles
            .Include(a => a.FeaturedImage)
            .Where(a => a.Published)
            .OrderByDescending(a => a.DatePublished)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        => await context.Articles.CountAsync(a => a.Published, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await context.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.ArticleId != excludeId), cancellationToken);

    public async Task<bool> AnyByFeaturedImageIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default)
        => await context.Articles.AnyAsync(a => a.FeaturedImageId == digitalAssetId, cancellationToken);

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
        => await context.Articles.AddAsync(article, cancellationToken);

    public void Update(Article article) => context.Articles.Update(article);
    public void Remove(Article article) => context.Articles.Remove(article);
}
