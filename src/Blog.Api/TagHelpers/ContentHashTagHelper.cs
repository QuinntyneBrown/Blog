using Blog.Api.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Blog.Api.TagHelpers;

/// <summary>
/// Rewrites the href or src attribute of link/script elements with a content-hashed
/// filename for aggressive immutable caching.
///
/// Usage:
///   &lt;link rel="stylesheet" href="/css/app.css" content-hash /&gt;
///   &lt;script src="/js/search.js" content-hash&gt;&lt;/script&gt;
///
/// Output:
///   &lt;link rel="stylesheet" href="/css/app.a1b2c3d4.css" /&gt;
///   &lt;script src="/js/search.a1b2c3d4.js"&gt;&lt;/script&gt;
/// </summary>
[HtmlTargetElement("link", Attributes = "content-hash")]
[HtmlTargetElement("script", Attributes = "content-hash")]
public class ContentHashTagHelper : TagHelper
{
    private readonly IContentHashService _hashService;

    [HtmlAttributeName("content-hash")]
    public bool ContentHash { get; set; }

    public ContentHashTagHelper(IContentHashService hashService)
    {
        _hashService = hashService;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Remove the content-hash attribute from the output
        output.Attributes.RemoveAll("content-hash");

        // Hash the href attribute (for <link>)
        if (output.Attributes.TryGetAttribute("href", out var hrefAttr) && hrefAttr.Value is string href)
        {
            var hashedHref = _hashService.GetHashedPath(href);
            output.Attributes.SetAttribute("href", hashedHref);
        }

        // Hash the src attribute (for <script>)
        if (output.Attributes.TryGetAttribute("src", out var srcAttr) && srcAttr.Value is string src)
        {
            var hashedSrc = _hashService.GetHashedPath(src);
            output.Attributes.SetAttribute("src", hashedSrc);
        }
    }
}
