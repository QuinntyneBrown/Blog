using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Blog.Api.Services;

/// <summary>
/// Concrete implementation of <see cref="IImageVariantGenerator"/> using
/// SixLabors.ImageSharp to produce WebP variants at the five responsive breakpoints
/// specified in the design (docs/detailed-designs/04-digital-asset-management/README.md,
/// Section 3.4 — ImageProcessor).
///
/// Note: The design also specifies AVIF variants, but SixLabors.ImageSharp 3.1.x does not
/// include a built-in AVIF encoder. WebP variants are generated here; AVIF support can be
/// added when an AVIF-capable encoder is available in the installed version of ImageSharp.
/// The serving endpoint already handles graceful fall-through from AVIF to WebP.
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

            var variantFileName = $"{assetId}-{breakpointWidth}w.webp";
            var variantPath = Path.Combine(assetsDir, variantFileName);

            try
            {
                // Clone so the resize for one breakpoint does not affect subsequent iterations.
                using var clone = source.Clone(ctx =>
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(breakpointWidth, 0), // height = 0 → preserve aspect ratio
                        Mode = ResizeMode.Max,
                    }));

                await clone.SaveAsync(variantPath, new WebpEncoder { Quality = 80 }, cancellationToken);

                logger.LogDebug(
                    "Generated WebP variant at {Width}px for asset {AssetId}: {FileName}",
                    breakpointWidth, assetId, variantFileName);
            }
            catch (Exception ex)
            {
                // A failure to generate one variant must not abort the upload.
                // Log and continue so other variants and the original asset are unaffected.
                logger.LogWarning(ex,
                    "Failed to generate WebP variant at {Width}px for asset {AssetId}",
                    breakpointWidth, assetId);
            }
        }
    }
}
