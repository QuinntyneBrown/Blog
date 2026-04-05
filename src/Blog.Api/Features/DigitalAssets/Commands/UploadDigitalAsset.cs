using Blog.Api.Common.Exceptions;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data;
using Blog.Api.Features.DigitalAssets.Queries;
using MediatR;
using SixLabors.ImageSharp;
using System.Security.Claims;

namespace Blog.Api.Features.DigitalAssets.Commands;

public record UploadDigitalAssetCommand(IFormFile File, Guid UserId) : IRequest<DigitalAssetDto>;

public class UploadDigitalAssetCommandHandler(IUnitOfWork uow, IWebHostEnvironment env) : IRequestHandler<UploadDigitalAssetCommand, DigitalAssetDto>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const int MaxDimension = 8192;
    private const long MaxPixelCount = 40_000_000; // 40 megapixels

    public async Task<DigitalAssetDto> Handle(UploadDigitalAssetCommand request, CancellationToken cancellationToken)
    {
        if (request.File.Length > MaxFileSize)
            throw new FileTooLargeException("File size exceeds the 10 MB limit.");

        var (contentType, extension) = await DetectContentTypeAsync(request.File);
        if (contentType == null)
            throw new ConflictException("File type not allowed. Supported types: JPEG, PNG, WebP, GIF, AVIF.");
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var assetsPath = Path.Combine(env.WebRootPath, "assets");
        Directory.CreateDirectory(assetsPath);
        var filePath = Path.Combine(assetsPath, storedFileName);

        using (var stream = request.File.OpenReadStream())
        {
            using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        using var image = await Image.LoadAsync(filePath, cancellationToken);
        var width = image.Width;
        var height = image.Height;

        if (width > MaxDimension || height > MaxDimension)
            throw new ConflictException($"Image dimensions ({width}x{height}) exceed the maximum of {MaxDimension}x{MaxDimension}.");
        if ((long)width * height > MaxPixelCount)
            throw new ConflictException($"Image pixel count ({(long)width * height:N0}) exceeds the maximum of {MaxPixelCount:N0}.");

        var asset = new DigitalAsset
        {
            DigitalAssetId = Guid.NewGuid(),
            OriginalFileName = request.File.FileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
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
