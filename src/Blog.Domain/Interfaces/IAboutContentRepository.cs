using Blog.Domain.Entities;

namespace Blog.Domain.Interfaces;

public interface IAboutContentRepository
{
    Task<AboutContent?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AboutContent entity, CancellationToken cancellationToken = default);
    void Update(AboutContent entity);
    Task AddHistoryAsync(AboutContentHistory entity, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AboutContentHistory> Items, int TotalCount)> GetHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AboutContentHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default);
}
