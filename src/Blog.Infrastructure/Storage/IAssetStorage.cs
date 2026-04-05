namespace Blog.Infrastructure.Storage;

public interface IAssetStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);
    string GetUrl(string storedFileName);
    bool Exists(string storedFileName);
}
