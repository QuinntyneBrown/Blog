using Blog.Api.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Blog.Api.TagHelpers;

/// <summary>
/// Inlines critical CSS from a stylesheet into a &lt;style&gt; element and
/// asynchronously loads the full stylesheet using rel="preload" with a
/// &lt;noscript&gt; fallback for users without JavaScript.
/// Usage: &lt;critical-css path="/css/app.css" nonce="@cspNonce" /&gt;
/// </summary>
[HtmlTargetElement("critical-css", TagStructure = TagStructure.WithoutEndTag)]
public class CriticalCssTagHelper : TagHelper
{
    private readonly ICriticalCssService _cssService;
    private readonly IContentHashService _hashService;

    [HtmlAttributeName("path")]
    public string Path { get; set; } = "";

    [HtmlAttributeName("nonce")]
    public string Nonce { get; set; } = "";

    public CriticalCssTagHelper(ICriticalCssService cssService, IContentHashService hashService)
    {
        _cssService = cssService;
        _hashService = hashService;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        if (string.IsNullOrEmpty(Path))
            return;

        var criticalCss = _cssService.GetCriticalCss(Path);
        var hashedPath = _hashService.GetHashedPath(Path);
        var nonceAttr = !string.IsNullOrEmpty(Nonce) ? $" nonce=\"{Nonce}\"" : "";

        if (!string.IsNullOrEmpty(criticalCss))
        {
            // Inline the critical CSS
            output.Content.AppendHtml($"<style{nonceAttr}>{criticalCss}</style>\n");
        }

        // Async-load the full stylesheet using preload pattern
        output.Content.AppendHtml(
            $"<link rel=\"preload\" href=\"{hashedPath}\" as=\"style\" onload=\"this.onload=null;this.rel='stylesheet'\"" +
            $"{nonceAttr} />\n");

        // Fallback for no-JS: standard blocking stylesheet
        output.Content.AppendHtml(
            $"<noscript><link rel=\"stylesheet\" href=\"{hashedPath}\"{nonceAttr} /></noscript>");
    }
}
