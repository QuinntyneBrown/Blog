namespace Blog.Api.Services;

/// <summary>
/// Generates responsive WebP and AVIF image variants at defined breakpoints
/// for an uploaded asset.
/// Design reference: docs/detailed-designs/04-digital-asset-management/README.md,
/// Section 3.4 — ImageProcessor, Section 5.1 — Upload Flow (step 11).
/// </summary>
public interface IImageVariantGenerator
{
    /// <summary>
    /// Eagerly generates WebP and AVIF variants at each responsive breakpoint
    /// (320, 640, 960, 1280, 1920) that is narrower than the original image width.
    /// Variant files are written to the same assets directory as the original.
    /// Naming convention: {assetId}-{width}w.{format} (e.g. a1b2c3-640w.webp).
    /// </summary>
    /// <param name="sourceFilePath">Absolute path to the original uploaded file.</param>
    /// <param name="assetId">The GUID identifier of the asset (used in variant filenames).</param>
    /// <param name="originalWidth">Width of the original image in pixels; variants wider than this are skipped.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task GenerateVariantsAsync(
        string sourceFilePath,
        Guid assetId,
        int originalWidth,
        CancellationToken cancellationToken = default);
}
