using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Seo;

public class SitemapTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SitemapTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSitemap_Returns200()
    {
        var response = await _client.GetAsync("/sitemap.xml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSitemap_ReturnsXml()
    {
        var response = await _client.GetAsync("/sitemap.xml");

        response.Content.Headers.ContentType!.MediaType.Should().Contain("xml");
    }

    [Fact]
    public async Task GetSitemap_ContainsUrlset()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("urlset");
    }

    [Fact]
    public async Task GetSitemap_ContainsArticleUrls()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("/articles/hello-world");
    }

    [Fact]
    public async Task GetSitemap_ContainsHomepageUrl()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("<loc>");
        body.Should().Contain("<priority>1.0</priority>");
    }
}
