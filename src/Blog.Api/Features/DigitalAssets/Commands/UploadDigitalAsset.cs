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
            asset.DigitalAssetId, asset.OriginalFileName,
            asset.ContentType, asset.FileSizeBytes, asset.Width, asset.Height,
            $"/assets/{asset.StoredFileName}", asset.CreatedAt);
    }

    private static async Task<(string? ContentType, string Extension)> DetectContentTypeAsync(IFormFile file)
    {
        var buffer = new byte[12];
        using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(buffer);
        if (bytesRead < 4)
            return (null, string.Empty);

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            return ("image/jpeg", ".jpg");

        // PNG: 89 50 4E 47
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return ("image/png", ".png");

        // GIF: 47 49 46 38
        if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
            return ("image/gif", ".gif");

        // WebP: RIFF....WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
        if (bytesRead >= 12
            && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46
            && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
            return ("image/webp", ".webp");

        // AVIF: ISOBMFF with 'ftypavif' (ftyp at bytes 4-7, avif at bytes 8-11)
        if (bytesRead >= 12
            && buffer[4] == 0x66 && buffer[5] == 0x74 && buffer[6] == 0x79 && buffer[7] == 0x70
            && buffer[8] == 0x61 && buffer[9] == 0x76 && buffer[10] == 0x69 && buffer[11] == 0x66)
            return ("image/avif", ".avif");

        return (null, string.Empty);
    }
}
