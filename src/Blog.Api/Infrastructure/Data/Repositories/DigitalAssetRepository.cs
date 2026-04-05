using Blog.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Infrastructure.Data.Repositories;

public class DigitalAssetRepository(BlogDbContext context) : IDigitalAssetRepository
{
    public async Task<DigitalAsset?> GetByIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default)
        => await context.DigitalAssets.FindAsync([digitalAssetId], cancellationToken);

    public async Task<List<DigitalAsset>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.DigitalAssets.OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(DigitalAsset digitalAsset, CancellationToken cancellationToken = default)
        => await context.DigitalAssets.AddAsync(digitalAsset, cancellationToken);

    public void Remove(DigitalAsset digitalAsset) => context.DigitalAssets.Remove(digitalAsset);
}
