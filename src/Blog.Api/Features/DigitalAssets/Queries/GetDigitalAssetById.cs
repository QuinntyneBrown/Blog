using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;

using MediatR;

namespace Blog.Api.Features.DigitalAssets.Queries;

public record GetDigitalAssetByIdQuery(Guid Id) : IRequest<DigitalAssetDto>;

public class GetDigitalAssetByIdHandler(IDigitalAssetRepository assets) : IRequestHandler<GetDigitalAssetByIdQuery, DigitalAssetDto>
{
    public async Task<DigitalAssetDto> Handle(GetDigitalAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await assets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Digital asset with ID '{request.Id}' was not found.");

        return new DigitalAssetDto(
            asset.DigitalAssetId, asset.OriginalFileName,
            asset.ContentType, asset.FileSizeBytes, asset.Width, asset.Height,
            $"/assets/{asset.StoredFileName}", asset.CreatedAt);
    }
}
