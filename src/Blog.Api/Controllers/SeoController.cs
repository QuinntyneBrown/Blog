using Blog.Api.Common.Attributes;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;

namespace Blog.Api.Controllers;

[Route("")]
[ApiController]
[RawResponse]
public class SeoController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    private string BaseUrl => configuration["Site:SiteUrl"]!.TrimEnd('/');
    private string SiteName => configuration["Site:SiteName"] ?? "Quinn Brown";
    private string SiteDescription => configuration["Site:SiteDescription"] ?? "Personal blog";
    private string AuthorName => configuration["Site:AuthorName"] ?? "Quinn Brown";

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var content = $"""
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /api/

            Sitemap: {BaseUrl}/sitemap.xml
            """;
        return Content(content, "text/plain");
    }

    [HttpGet("llms.txt")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> LlmsTxt()
    {
        var result = await mediator.Send(new GetPublishedArticlesQuery(1, 100));
        var sb = new StringBuilder();
        sb.AppendLine($"# {SiteName}'s Blog");
        sb.AppendLine();
        sb.AppendLine(SiteDescription);
        sb.AppendLine();
        sb.AppendLine("## Articles");
        sb.AppendLine();
        foreach (var a in result.Items)
            sb.AppendLine($"- [{a.Title}]({BaseUrl}/articles/{a.Slug}) — {a.Abstract}");
        sb.AppendLine();
        sb.AppendLine("## Feeds");
        sb.AppendLine($"- RSS: {BaseUrl}/feed.xml");
        sb.AppendLine($"- Atom: {BaseUrl}/atom.xml");
        sb.AppendLine($"- JSON: {BaseUrl}/feed/json");
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var result = await mediator.Send(new GetPublishedArticlesQuery(1, 1000));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<XElement>
        {
            new XElement(ns + "url",
                new XElement(ns + "loc", $"{BaseUrl}/"),
                new XElement(ns + "changefreq", "daily"),
                new XElement(ns + "priority", "1.0"))
        };

        foreach (var article in result.Items)
        {
            urls.Add(new XElement(ns + "url",
                new XElement(ns + "loc", $"{BaseUrl}/articles/{article.Slug}"),
                new XElement(ns + "lastmod", article.UpdatedAt.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", "0.8")));
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset", urls));

        return Content(doc.ToString(), "application/xml; charset=utf-8");
    }

    [HttpGet("feed.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Rss()
    {
        var result = await mediator.Send(new GetPublishedArticlesQuery(1, 20));
        XNamespace dc = "http://purl.org/dc/elements/1.1/";
        XNamespace content = "http://purl.org/rss/1.0/modules/content/";
        var items = result.Items.Select(a =>
            new XElement("item",
                new XElement("title", a.Title),
                new XElement("link", $"{BaseUrl}/articles/{a.Slug}"),
                new XElement("description", a.Abstract),
                new XElement("pubDate", a.DatePublished?.ToString("R")),
                new XElement(dc + "creator", AuthorName),
                new XElement("guid", $"{BaseUrl}/articles/{a.Slug}")));

        var channel = new XElement("channel",
            new XElement("title", SiteName),
            new XElement("link", BaseUrl),
            new XElement("description", SiteDescription),
            new XElement("language", "en-us"),
            new XElement("lastBuildDate", DateTime.UtcNow.ToString("R")),
            new XElement("atom:link",
                new XAttribute(XNamespace.Xmlns + "atom", "http://www.w3.org/2005/Atom"),
                new XAttribute("href", $"{BaseUrl}/feed.xml"),
                new XAttribute("rel", "self"),
                new XAttribute("type", "application/rss+xml")));
        channel.Add(items);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "dc", dc),
                new XAttribute(XNamespace.Xmlns + "content", content),
                channel));

        return Content(doc.ToString(), "application/rss+xml; charset=utf-8");
    }

    [HttpGet("atom.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Atom()
    {
        var result = await mediator.Send(new GetPublishedArticlesQuery(1, 20));
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var entries = result.Items.Select(a =>
            new XElement(atom + "entry",
                new XElement(atom + "id", $"{BaseUrl}/articles/{a.Slug}"),
                new XElement(atom + "title", a.Title),
                new XElement(atom + "summary", a.Abstract),
                new XElement(atom + "link",
                    new XAttribute("href", $"{BaseUrl}/articles/{a.Slug}"),
                    new XAttribute("rel", "alternate")),
                new XElement(atom + "published", a.DatePublished?.ToString("O") ?? a.CreatedAt.ToString("O")),
                new XElement(atom + "updated", a.UpdatedAt.ToString("O")),
                new XElement(atom + "author", new XElement(atom + "name", "Quinn Brown"))));

        var feed = new XElement(atom + "feed",
            new XElement(atom + "id", BaseUrl),
            new XElement(atom + "title", "Quinn Brown"),
            new XElement(atom + "subtitle", "Thoughts on software engineering, .NET architecture, and building systems that last."),
            new XElement(atom + "link", new XAttribute("href", BaseUrl)),
            new XElement(atom + "link",
                new XAttribute("href", $"{BaseUrl}/atom.xml"),
                new XAttribute("rel", "self")),
            new XElement(atom + "updated", DateTime.UtcNow.ToString("O")),
            new XElement(atom + "author", new XElement(atom + "name", "Quinn Brown")));
        feed.Add(entries);

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), feed);
        return Content(doc.ToString(), "application/atom+xml; charset=utf-8");
    }

    [HttpGet("feed/json")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> JsonFeed()
    {
        var result = await mediator.Send(new GetPublishedArticlesQuery(1, 20));
        var feed = new
        {
            version = "https://jsonfeed.org/version/1.1",
            title = "Quinn Brown",
            home_page_url = BaseUrl,
            feed_url = $"{BaseUrl}/feed/json",
            description = "Thoughts on software engineering, .NET architecture, and building systems that last.",
            language = "en-US",
            items = result.Items.Select(a => new
            {
                id = $"{BaseUrl}/articles/{a.Slug}",
                url = $"{BaseUrl}/articles/{a.Slug}",
                title = a.Title,
                summary = a.Abstract,
                date_published = a.DatePublished?.ToString("O"),
                date_modified = a.UpdatedAt.ToString("O")
            })
        };
        return Ok(feed);
    }
}
