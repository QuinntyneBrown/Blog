using Ganss.Xss;
using Markdig;
using HtmlSanitizer = Ganss.Xss.HtmlSanitizer;

namespace Blog.Api.Services;

public class MarkdownConverter : IMarkdownConverter
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly HtmlSanitizer _sanitizer = new();

    public string Convert(string markdown)
    {
        var html = Markdown.ToHtml(markdown, _pipeline);
        return _sanitizer.Sanitize(html);
    }
}
