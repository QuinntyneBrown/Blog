using Microsoft.Extensions.Configuration;

namespace Blog.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation — placeholder for future cloud deployment.
/// Configure by setting AssetStorage:Provider = "Blob" in appsettings.
/// </summary>
public class BlobAssetStorage(IConfiguration configuration) : IAssetStorage
{
    private readonly string _containerUrl = configuration["AssetStorage:BlobContainerUrl"] ?? string.Empty;

    public Task SaveAsync(string storedFileName, Stream stream, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Azure Blob Storage not yet configured. Set AssetStorage:Provider = 'Local' or configure blob connection string.");

    public string GetFilePath(string storedFileName)
        => throw new NotImplementedException("Azure Blob Storage does not support local file paths. Use GetUrl for cloud storage.");

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Azure Blob Storage not yet configured.");

    public string GetUrl(string storedFileName) => $"{_containerUrl}/{storedFileName}";

    public bool Exists(string storedFileName)
        => throw new NotImplementedException("Azure Blob Storage not yet configured.");
}
