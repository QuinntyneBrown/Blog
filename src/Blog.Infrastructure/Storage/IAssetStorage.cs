namespace Blog.Infrastructure.Storage;

public interface IAssetStorage
{
    /// <summary>Saves a stream to storage under <paramref name="storedFileName"/>.</summary>
    Task SaveAsync(string storedFileName, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>Returns a readable stream for the file, or <c>null</c> if the file does not exist.
    /// Design reference: docs/detailed-designs/04-digital-asset-management/README.md Section 3.5.</summary>
    Task<Stream?> GetAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Returns the absolute filesystem path for the given stored filename,
    /// allowing image-processing libraries (e.g. ImageSharp) to write to it directly.
    /// Not meaningful for cloud storage implementations.</summary>
    string GetFilePath(string storedFileName);

    /// <summary>Deletes the file with the given stored filename from storage.</summary>
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>Returns the public-facing URL for the given stored filename.</summary>
    string GetUrl(string storedFileName);

    /// <summary>Returns true when the stored file exists in the backing store.</summary>
    bool Exists(string storedFileName);
}
