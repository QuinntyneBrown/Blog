using Blog.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Infrastructure.Data.Repositories;

public class ArticleRepository(BlogDbContext context) : IArticleRepository
{
    public async Task<Article?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken = default)
        => await context.Articles.Include(a => a.FeaturedImage).FirstOrDefaultAsync(a => a.ArticleId == articleId, cancellationToken);

    public async Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Articles.Include(a => a.FeaturedImage).FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);

    public async Task<(List<Article> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Articles.OrderByDescending(a => a.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(List<Article> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Articles.Where(a => a.Published).OrderByDescending(a => a.DatePublished);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await context.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.ArticleId != excludeId), cancellationToken);

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
        => await context.Articles.AddAsync(article, cancellationToken);

    public void Update(Article article) => context.Articles.Update(article);

    public void Remove(Article article) => context.Articles.Remove(article);
}
