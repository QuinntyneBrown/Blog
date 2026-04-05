using Blog.Domain.Entities;

namespace Blog.Domain.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Article>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetAllCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Article> Articles, int TotalCount)> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Article>> GetSuggestionsAsync(string query, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> AnyByFeaturedImageIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default);
    Task AddAsync(Article article, CancellationToken cancellationToken = default);
    void Update(Article article);
    void Remove(Article article);
}
