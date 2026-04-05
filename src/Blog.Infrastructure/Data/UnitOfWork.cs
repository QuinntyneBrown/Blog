using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Blog.Infrastructure.Data;

public class UnitOfWork(BlogDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public IArticleRepository Articles { get; } = new ArticleRepository(context);
    public IUserRepository Users { get; } = new UserRepository(context);
    public IDigitalAssetRepository DigitalAssets { get; } = new DigitalAssetRepository(context);
    public IAboutContentRepository AboutContents { get; } = new AboutContentRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null) await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null) await _transaction.RollbackAsync(cancellationToken);
    }
}
