namespace Blog.Infrastructure.Storage;

public interface IAssetStorage
{
    /// <summary>Saves a stream to storage under <paramref name="storedFileName"/>.</summary>
    Task SaveAsync(string storedFileName, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>Returns the absolute filesystem path for the given stored filename,
    /// allowing image-processing libraries to write to it directly.</summary>
    string GetFilePath(string storedFileName);

    /// <summary>Deletes the file with the given stored filename from storage.</summary>
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Returns the public-facing URL for the given stored filename.</summary>
    string GetUrl(string storedFileName);

    /// <summary>Returns true when the stored file exists in the backing store.</summary>
    bool Exists(string storedFileName);
}
