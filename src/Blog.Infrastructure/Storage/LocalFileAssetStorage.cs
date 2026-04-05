using Microsoft.Extensions.Configuration;

namespace Blog.Infrastructure.Storage;

public class LocalFileAssetStorage(IConfiguration configuration) : IAssetStorage
{
    private string StoragePath => configuration["AssetStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets");
    private string BaseUrl => configuration["AssetStorage:BaseUrl"] ?? "/assets";

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(StoragePath);
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(StoragePath, storedFileName);

        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return storedFileName;
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(StoragePath, storedFileName);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string GetUrl(string storedFileName) => $"{BaseUrl}/{storedFileName}";

    public bool Exists(string storedFileName) => File.Exists(Path.Combine(StoragePath, storedFileName));
}
