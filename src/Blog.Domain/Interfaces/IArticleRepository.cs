using Blog.Domain.Entities;

namespace Blog.Domain.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<(List<Article> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(List<Article> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> AnyByFeaturedImageIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default);
    Task AddAsync(Article article, CancellationToken cancellationToken = default);
    void Update(Article article);
    void Remove(Article article);
}
