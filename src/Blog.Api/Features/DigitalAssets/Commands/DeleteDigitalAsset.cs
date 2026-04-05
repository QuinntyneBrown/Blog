using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;
using Blog.Infrastructure.Data;
using Blog.Infrastructure.Storage;
using MediatR;

namespace Blog.Api.Features.DigitalAssets.Commands;

public record DeleteDigitalAssetCommand(Guid Id) : IRequest;

public class DeleteDigitalAssetCommandHandler(IUnitOfWork uow, IAssetStorage assetStorage) : IRequestHandler<DeleteDigitalAssetCommand>
{
    public async Task Handle(DeleteDigitalAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await uow.DigitalAssets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Digital asset with ID '{request.Id}' was not found.");

        if (await uow.Articles.AnyByFeaturedImageIdAsync(request.Id, cancellationToken))
            throw new ConflictException("Cannot delete this asset because it is referenced by one or more articles.");

        // Delete the stored file via IAssetStorage (design Section 3.5 — DeleteAsync).
        await assetStorage.DeleteAsync(asset.StoredFileName, cancellationToken);

        uow.DigitalAssets.Remove(asset);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
