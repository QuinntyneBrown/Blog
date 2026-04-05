using Blog.Api.Domain.Entities;

namespace Blog.Api.Infrastructure.Data.Repositories;

public interface IDigitalAssetRepository
{
    Task<DigitalAsset?> GetByIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default);
    Task<List<DigitalAsset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(DigitalAsset digitalAsset, CancellationToken cancellationToken = default);
    void Remove(DigitalAsset digitalAsset);
}
