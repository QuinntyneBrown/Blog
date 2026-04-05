using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Blog.Api.TagHelpers;

/// <summary>
/// Transforms an &lt;img&gt; element into a &lt;picture&gt; element with AVIF and WebP
/// sources, responsive srcset attributes, and lazy-loading for below-fold images.
///
/// Usage:
///   &lt;img responsive src="/assets/{id}.jpg"
///        asset-id="{guid}" alt="Title"
///        img-width="1280" img-height="720"
///        breakpoints="320,640,960,1280,1920"
///        sizes="(max-width: 768px) 100vw, 1280px"
///        priority="true" /&gt;
///
/// When the "responsive" attribute is present, this tag helper activates and
/// wraps the image in a picture element with format variants.
/// </summary>
[HtmlTargetElement("img", Attributes = "responsive")]
public class ResponsiveImageTagHelper : TagHelper
{
    private static readonly int[] DefaultBreakpoints = [320, 640, 960, 1280, 1920];
    private static readonly int[] CardBreakpoints = [320, 640, 960];

    [HtmlAttributeName("src")]
    public string Src { get; set; } = "";

    [HtmlAttributeName("asset-id")]
    public string? AssetId { get; set; }

    [HtmlAttributeName("alt")]
    public string Alt { get; set; } = "";

    [HtmlAttributeName("img-width")]
    public int? ImgWidth { get; set; }

    [HtmlAttributeName("img-height")]
    public int? ImgHeight { get; set; }

    [HtmlAttributeName("breakpoints")]
    public string? Breakpoints { get; set; }

    [HtmlAttributeName("sizes")]
    public string Sizes { get; set; } = "(max-width: 768px) 100vw, (max-width: 1200px) 960px, 1280px";

    /// <summary>
    /// When true, uses loading="eager" and fetchpriority="high" (above-fold hero images).
    /// When false, uses loading="lazy" and decoding="async" (below-fold card images).
    /// </summary>
    [HtmlAttributeName("priority")]
    public bool Priority { get; set; }

    [HtmlAttributeName("responsive")]
    public bool Responsive { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Responsive || string.IsNullOrEmpty(Src))
            return;

        // Resolve the asset ID from the src path if not explicitly provided
        var resolvedAssetId = AssetId;
        if (string.IsNullOrEmpty(resolvedAssetId) && Src.StartsWith("/assets/"))
        {
            var fileName = Src["/assets/".Length..];
            resolvedAssetId = Path.GetFileNameWithoutExtension(fileName);
        }

        if (string.IsNullOrEmpty(resolvedAssetId))
            return; // Cannot generate responsive variants without an asset ID

        // Parse breakpoints
        var breakpoints = ParseBreakpoints();

        // Build srcsets
        var avifSrcset = string.Join(", ",
            breakpoints.Select(w => $"/assets/{resolvedAssetId}-{w}w.avif {w}w"));
        var webpSrcset = string.Join(", ",
            breakpoints.Select(w => $"/assets/{resolvedAssetId}-{w}w.webp {w}w"));

        // Build dimension attributes
        var dimensionAttrs = "";
        if (ImgWidth.HasValue && ImgHeight.HasValue)
            dimensionAttrs = $" width=\"{ImgWidth}\" height=\"{ImgHeight}\"";

        // Loading strategy
        var loadingAttrs = Priority
            ? " loading=\"eager\" fetchpriority=\"high\" decoding=\"async\""
            : " loading=\"lazy\" decoding=\"async\"";

        // Replace <img> with <picture>
        output.TagName = null;
        output.Content.SetHtmlContent(
            $"<picture>\n" +
            $"    <source type=\"image/avif\" srcset=\"{avifSrcset}\" sizes=\"{Sizes}\" />\n" +
            $"    <source type=\"image/webp\" srcset=\"{webpSrcset}\" sizes=\"{Sizes}\" />\n" +
            $"    <img src=\"{Src}\" alt=\"{Alt}\"{dimensionAttrs}{loadingAttrs} />\n" +
            $"</picture>");
    }

    private int[] ParseBreakpoints()
    {
        if (string.IsNullOrEmpty(Breakpoints))
            return Priority ? DefaultBreakpoints : CardBreakpoints;

        return Breakpoints.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .OrderBy(x => x)
            .ToArray();
    }
}
