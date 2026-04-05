using Blog.Api.Common.Attributes;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

/// <summary>
/// Serves uploaded digital assets with content negotiation (WebP/AVIF preferred based on
/// Accept header) and optional width-based variant selection via the ?w= query parameter.
/// Design reference: docs/detailed-designs/04-digital-asset-management/README.md,
/// Section 3.5 — AssetStorage (GetAsync / GetFilePath),
/// Section 3.6 — AssetRepository (GetByStoredFileNameAsync),
/// Section 5.2 — Serve with Optimization Flow, Section 6.3 — GET /assets/{filename}.
/// </summary>
[Route("assets")]
[ApiController]
[RawResponse]
public class AssetsController(IAssetStorage assetStorage, IDigitalAssetRepository assetRepository) : ControllerBase
{
    // Responsive breakpoints in ascending order (matches ImageVariantGenerator breakpoints).
    private static readonly int[] Breakpoints = [320, 640, 960, 1280, 1920];

    [HttpGet("{fileName}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Serve(string fileName, [FromQuery] int? w, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
            return BadRequest();

        // Validate that the requested filename corresponds to a registered DigitalAsset entity.
        // Design reference: Section 3.6 — GetByStoredFileNameAsync is "used during serve".
        // Only the base stored filename (the original uploaded file) has an entity record;
        // variant files ({assetId}-{width}w.{format}) are derived from that entity.
        var baseStoredFileName = Path.GetFileName(fileName);
        var asset = await assetRepository.GetByStoredFileNameAsync(baseStoredFileName, ct);
        if (asset == null)
            return NotFound();

        // Determine the best format accepted by the client (AVIF > WebP > original).
        var accept = Request.Headers.Accept.ToString();
        var preferAvif  = accept.Contains("image/avif",  StringComparison.OrdinalIgnoreCase);
        var preferWebp  = accept.Contains("image/webp",  StringComparison.OrdinalIgnoreCase);

        // If a width was requested and a variant-capable format is accepted, try to find
        // a pre-generated variant that matches the request.
        // Variants are derived from the confirmed asset's GUID (entity ID == stored filename base).
        var assetId = asset.DigitalAssetId;
        if (w.HasValue && (preferAvif || preferWebp))
        {
            var nearestWidth = FindNearestBreakpoint(w.Value);

            // Try AVIF first (highest compression), then WebP.
            // Use AssetStorage.GetAsync() per the design (Section 5.2, step 5) so the serving
            // path works regardless of whether the backing store is local filesystem or cloud blob.
            if (preferAvif)
            {
                var avifVariantName = $"{assetId}-{nearestWidth}w.avif";
                var avifStream = await assetStorage.GetAsync(avifVariantName, ct);
                if (avifStream != null)
                    return await ServeStreamAsync(avifStream, avifVariantName, "image/avif");
            }

            if (preferWebp)
            {
                var webpVariantName = $"{assetId}-{nearestWidth}w.webp";
                var webpStream = await assetStorage.GetAsync(webpVariantName, ct);
                if (webpStream != null)
                    return await ServeStreamAsync(webpStream, webpVariantName, "image/webp");
            }
        }

        // Fall back to serving the exact filename requested via AssetStorage.GetAsync().
        var fallbackStream = await assetStorage.GetAsync(fileName, ct);
        if (fallbackStream == null)
            return NotFound();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".webp"           => "image/webp",
            ".avif"           => "image/avif",
            ".gif"            => "image/gif",
            ".svg"            => "image/svg+xml",
            _                 => "application/octet-stream"
        };

        return await ServeStreamAsync(fallbackStream, fileName, contentType);
    }

    private Task<IActionResult> ServeStreamAsync(Stream stream, string name, string contentType)
    {
        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        // Vary: Accept ensures caches distinguish between format-negotiated variants.
        Response.Headers["Vary"] = "Accept";

        var etag = $"\"{name}\"";
        Response.Headers["ETag"] = etag;

        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            stream.Dispose();
            return Task.FromResult<IActionResult>(StatusCode(304));
        }

        return Task.FromResult<IActionResult>(File(stream, contentType));
    }

    /// <summary>
    /// Returns the nearest breakpoint that is &gt;= the requested width,
    /// or the largest breakpoint if the requested width exceeds all breakpoints.
    /// </summary>
    private static int FindNearestBreakpoint(int requestedWidth)
    {
        foreach (var bp in Breakpoints)
            if (bp >= requestedWidth)
                return bp;
        return Breakpoints[^1];
    }
}
