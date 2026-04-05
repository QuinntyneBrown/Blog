using Blog.Domain.Interfaces;
using Blog.Api.Common.Exceptions;
using Blog.Infrastructure.Data;
using MediatR;

namespace Blog.Api.Features.DigitalAssets.Commands;

public record DeleteDigitalAssetCommand(Guid Id) : IRequest;

public class DeleteDigitalAssetCommandHandler(IUnitOfWork uow, IWebHostEnvironment env) : IRequestHandler<DeleteDigitalAssetCommand>
{
    public async Task Handle(DeleteDigitalAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await uow.DigitalAssets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Digital asset with ID '{request.Id}' was not found.");

        if (await uow.Articles.AnyByFeaturedImageIdAsync(request.Id, cancellationToken))
            throw new ConflictException("Cannot delete this asset because it is referenced by one or more articles.");

        var filePath = Path.Combine(env.WebRootPath, "assets", asset.StoredFileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        uow.DigitalAssets.Remove(asset);
        await uow.SaveChangesAsync(cancellationToken);
    }
}
