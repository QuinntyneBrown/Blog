using NeoSolve.ImageSharp.AVIF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Blog.Api.Services;

/// <summary>
/// Concrete implementation of <see cref="IImageVariantGenerator"/> using
/// SixLabors.ImageSharp to produce WebP and AVIF variants at the five responsive breakpoints
/// specified in the design (docs/detailed-designs/04-digital-asset-management/README.md,
/// Section 3.4 — ImageProcessor).
///
/// AVIF encoding is provided by BrycensRanch.NeoSolve.ImageSharp.AVIF which delegates to
/// the avifenc CLI tool. If avifenc is not installed, AVIF variant generation will fail
/// gracefully (logged as a warning) and the serving endpoint will fall through to WebP.
/// </summary>
public class ImageVariantGenerator(ILogger<ImageVariantGenerator> logger) : IImageVariantGenerator
{
    private static readonly int[] Breakpoints = [320, 640, 960, 1280, 1920];

    public async Task GenerateVariantsAsync(
        string sourceFilePath,
        Guid assetId,
        int originalWidth,
        CancellationToken cancellationToken = default)
    {
        var assetsDir = Path.GetDirectoryName(sourceFilePath)!;

        // Load the source image once; ImageSharp keeps it in memory for repeated resizes.
        using var source = await Image.LoadAsync(sourceFilePath, cancellationToken);

        foreach (var breakpointWidth in Breakpoints)
        {
            // Skip breakpoints that are wider than (or equal to) the original — no upscaling.
            if (breakpointWidth >= originalWidth)
                continue;

            // Clone so the resize for one breakpoint does not affect subsequent iterations.
            using var clone = source.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(breakpointWidth, 0), // height = 0 → preserve aspect ratio
                    Mode = ResizeMode.Max,
                }));

            // Generate WebP variant.
            await SaveVariantAsync(clone, assetsDir, assetId, breakpointWidth, "webp",
                new WebpEncoder { Quality = 80 }, cancellationToken);

            // Generate AVIF variant (design Section 3.4 requires both formats).
            await SaveVariantAsync(clone, assetsDir, assetId, breakpointWidth, "avif",
                new AVIFEncoder(), cancellationToken);
        }
    }

    private async Task SaveVariantAsync(
        Image clone,
        string assetsDir,
        Guid assetId,
        int breakpointWidth,
        string format,
        SixLabors.ImageSharp.Formats.IImageEncoder encoder,
        CancellationToken cancellationToken)
    {
        var variantFileName = $"{assetId}-{breakpointWidth}w.{format}";
        var variantPath = Path.Combine(assetsDir, variantFileName);

        try
        {
            await clone.SaveAsync(variantPath, encoder, cancellationToken);

            logger.LogDebug(
                "Generated {Format} variant at {Width}px for asset {AssetId}: {FileName}",
                format.ToUpperInvariant(), breakpointWidth, assetId, variantFileName);
        }
        catch (Exception ex)
        {
            // A failure to generate one variant must not abort the upload.
            // Log and continue so other variants and the original asset are unaffected.
            logger.LogWarning(ex,
                "Failed to generate {Format} variant at {Width}px for asset {AssetId}",
                format.ToUpperInvariant(), breakpointWidth, assetId);
        }
    }
}
