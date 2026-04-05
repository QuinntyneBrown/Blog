using Blog.Domain.Interfaces;

using MediatR;

namespace Blog.Api.Features.DigitalAssets.Queries;

public record DigitalAssetDto(
    Guid DigitalAssetId, string OriginalFileName, string StoredFileName,
    string ContentType, long FileSizeBytes, int Width, int Height,
    string Url, DateTime CreatedAt);

public record GetDigitalAssetsQuery : IRequest<List<DigitalAssetDto>>;

public class GetDigitalAssetsHandler(IDigitalAssetRepository assets) : IRequestHandler<GetDigitalAssetsQuery, List<DigitalAssetDto>>
{
    public async Task<List<DigitalAssetDto>> Handle(GetDigitalAssetsQuery request, CancellationToken cancellationToken)
    {
        var items = await assets.GetAllAsync(cancellationToken);
        return items.Select(d => new DigitalAssetDto(
            d.DigitalAssetId, d.OriginalFileName, d.StoredFileName,
            d.ContentType, d.FileSizeBytes, d.Width, d.Height,
            $"/assets/{d.StoredFileName}", d.CreatedAt)).ToList();
    }
}
