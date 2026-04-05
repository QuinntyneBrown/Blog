using Microsoft.Extensions.Configuration;

namespace Blog.Infrastructure.Storage;

public class LocalFileAssetStorage(IConfiguration configuration) : IAssetStorage
{
    private string StoragePath => configuration["AssetStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets");
    private string BaseUrl => configuration["AssetStorage:BaseUrl"] ?? "/assets";

    public async Task SaveAsync(string storedFileName, Stream stream, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(StoragePath);
        var filePath = Path.Combine(StoragePath, storedFileName);
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream?> GetAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(StoragePath, storedFileName);
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
        return Task.FromResult<Stream?>(stream);
    }

    public string GetFilePath(string storedFileName) => Path.Combine(StoragePath, storedFileName);

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(StoragePath, storedFileName);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string GetUrl(string storedFileName) => $"{BaseUrl}/{storedFileName}";

    public bool Exists(string storedFileName) => File.Exists(Path.Combine(StoragePath, storedFileName));
}
