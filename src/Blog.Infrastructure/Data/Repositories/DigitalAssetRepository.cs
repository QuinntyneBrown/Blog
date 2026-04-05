using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Data.Repositories;

public class DigitalAssetRepository(BlogDbContext context) : IDigitalAssetRepository
{
    public async Task<DigitalAsset?> GetByIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default)
        => await context.DigitalAssets.FindAsync([digitalAssetId], cancellationToken);

    public async Task<DigitalAsset?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellationToken = default)
        => await context.DigitalAssets.FirstOrDefaultAsync(d => d.StoredFileName == storedFileName, cancellationToken);

    public async Task<IReadOnlyList<DigitalAsset>> GetByCreatedByAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.DigitalAssets
            .Where(d => d.CreatedBy == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DigitalAsset digitalAsset, CancellationToken cancellationToken = default)
        => await context.DigitalAssets.AddAsync(digitalAsset, cancellationToken);

    public void Remove(DigitalAsset digitalAsset) => context.DigitalAssets.Remove(digitalAsset);
}
