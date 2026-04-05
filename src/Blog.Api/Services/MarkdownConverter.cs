using Ganss.Xss;
using Markdig;
using HtmlSanitizer = Ganss.Xss.HtmlSanitizer;

namespace Blog.Api.Services;

public class MarkdownConverter : IMarkdownConverter
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    // Design reference: docs/detailed-designs/08-security-hardening/README.md, Section 3.7
    // The sanitizer is configured with an explicit minimal allow-list of tags and attributes
    // matching exactly the set required for Markdown-derived article content, reducing the
    // XSS attack surface compared to Ganss.Xss's permissive defaults.
    private static readonly HtmlSanitizer Sanitizer = BuildSanitizer();

    private static HtmlSanitizer BuildSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // Replace the default tag allow-list with the minimal design-specified set.
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "h1", "h2", "h3", "h4", "h5", "h6",
            "a", "img",
            "ul", "ol", "li",
            "strong", "em",
            "code", "pre",
            "blockquote",
            "figure", "figcaption",
            "table", "thead", "tbody", "tr", "th", "td",
            "input"
        })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        // Retain only the attributes needed by the allowed tags.
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");   // <a>
        sanitizer.AllowedAttributes.Add("src");    // <img>
        sanitizer.AllowedAttributes.Add("alt");    // <img>
        sanitizer.AllowedAttributes.Add("title");  // <a>, <img>
        sanitizer.AllowedAttributes.Add("width");  // <img>
        sanitizer.AllowedAttributes.Add("height"); // <img>
        sanitizer.AllowedAttributes.Add("id");     // heading anchors
        sanitizer.AllowedAttributes.Add("type");    // <input type="checkbox"> (task lists)
        sanitizer.AllowedAttributes.Add("checked"); // <input checked> (task lists)
        sanitizer.AllowedAttributes.Add("disabled"); // <input disabled> (task lists)

        // Only allow safe URI schemes on href/src to block javascript: URLs.
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("mailto");

        return sanitizer;
    }

    public string Convert(string markdown)
    {
        var html = Markdown.ToHtml(markdown, _pipeline);
        return Sanitizer.Sanitize(html);
    }
}
