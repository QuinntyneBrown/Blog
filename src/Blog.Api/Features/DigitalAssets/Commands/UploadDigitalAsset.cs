using Blog.Api.Common.Exceptions;
using Blog.Api.Domain.Entities;
using Blog.Api.Infrastructure.Data;
using Blog.Api.Features.DigitalAssets.Queries;
using MediatR;
using SixLabors.ImageSharp;
using System.Security.Claims;

namespace Blog.Api.Features.DigitalAssets.Commands;

public record UploadDigitalAssetCommand(IFormFile File, Guid UserId) : IRequest<DigitalAssetDto>;

public class UploadDigitalAssetCommandHandler(IUnitOfWork uow, IWebHostEnvironment env) : IRequestHandler<UploadDigitalAssetCommand, DigitalAssetDto>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/avif"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public async Task<DigitalAssetDto> Handle(UploadDigitalAssetCommand request, CancellationToken cancellationToken)
    {
        if (request.File.Length > MaxFileSize)
            throw new FileTooLargeException("File size exceeds the 10 MB limit.");

        if (!AllowedTypes.Contains(request.File.ContentType.ToLowerInvariant()))
            throw new ConflictException($"File type '{request.File.ContentType}' is not allowed.");

        var extension = Path.GetExtension(request.File.FileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var assetsPath = Path.Combine(env.WebRootPath, "assets");
        Directory.CreateDirectory(assetsPath);
        var filePath = Path.Combine(assetsPath, storedFileName);

        int? width = null, height = null;

        using (var stream = request.File.OpenReadStream())
        {
            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        try
        {
            using var image = await Image.LoadAsync(filePath, cancellationToken);
            width = image.Width;
            height = image.Height;
        }
        catch { /* Not an image we can inspect */ }

        var asset = new DigitalAsset
        {
            DigitalAssetId = Guid.NewGuid(),
            OriginalFileName = request.File.FileName,
            StoredFileName = storedFileName,
            ContentType = request.File.ContentType,
            FileSizeBytes = request.File.Length,
            Width = width,
            Height = height,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId
        };

        await uow.DigitalAssets.AddAsync(asset, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return new DigitalAssetDto(
            asset.DigitalAssetId, asset.OriginalFileName, asset.StoredFileName,
            asset.ContentType, asset.FileSizeBytes, asset.Width, asset.Height,
            $"/assets/{asset.StoredFileName}", asset.CreatedAt);
    }
}
