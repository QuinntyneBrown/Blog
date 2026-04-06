namespace Blog.Domain.Interfaces;

public interface IUnitOfWork
{
    IArticleRepository Articles { get; }
    IEventRepository Events { get; }
    IUserRepository Users { get; }
    IDigitalAssetRepository DigitalAssets { get; }
    INewsletterRepository Newsletters { get; }
    IAboutContentRepository AboutContents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
