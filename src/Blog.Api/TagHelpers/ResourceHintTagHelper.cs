using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Blog.Api.TagHelpers;

/// <summary>
/// Generates resource hints (preconnect, dns-prefetch, preload) in the document head
/// to accelerate connections to critical third-party domains and preload key resources.
///
/// Usage:
///   &lt;resource-hint type="preconnect" href="https://fonts.googleapis.com" /&gt;
///   &lt;resource-hint type="preconnect" href="https://fonts.gstatic.com" crossorigin="true" /&gt;
///   &lt;resource-hint type="dns-prefetch" href="https://fonts.googleapis.com" /&gt;
///   &lt;resource-hint type="preload" href="/css/app.css" as="style" /&gt;
///   &lt;resource-hint type="preload" href="/fonts/inter.woff2" as="font" crossorigin="true" /&gt;
/// </summary>
[HtmlTargetElement("resource-hint", TagStructure = TagStructure.WithoutEndTag)]
public class ResourceHintTagHelper : TagHelper
{
    [HtmlAttributeName("type")]
    public string HintType { get; set; } = "preconnect";

    [HtmlAttributeName("href")]
    public string Href { get; set; } = "";

    [HtmlAttributeName("crossorigin")]
    public bool Crossorigin { get; set; }

    [HtmlAttributeName("as")]
    public string? As { get; set; }

    [HtmlAttributeName("nonce")]
    public string? Nonce { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        if (string.IsNullOrEmpty(Href))
            return;

        var crossoriginAttr = Crossorigin ? " crossorigin" : "";
        var asAttr = !string.IsNullOrEmpty(As) ? $" as=\"{As}\"" : "";
        var nonceAttr = !string.IsNullOrEmpty(Nonce) ? $" nonce=\"{Nonce}\"" : "";

        output.Content.SetHtmlContent(
            $"<link rel=\"{HintType}\" href=\"{Href}\"{asAttr}{crossoriginAttr}{nonceAttr} />");
    }
}
