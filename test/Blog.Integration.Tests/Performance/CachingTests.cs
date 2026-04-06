using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Performance;

public class CachingTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly BlogWebApplicationFactory _factory;

    public CachingTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetArticleById_ReturnsETagHeader()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var listResponse = await client.GetAsync("/api/articles");
        listResponse.EnsureSuccessStatusCode();
        var listBody = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = System.Text.Json.JsonDocument.Parse(listBody);
        var articleId = listDoc.RootElement.GetProperty("data").GetProperty("items")[0].GetProperty("articleId").GetString();

        var response = await client.GetAsync($"/api/articles/{articleId}");
        response.EnsureSuccessStatusCode();

        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.Tag.Should().StartWith("W/\"article-");
    }

    [Fact]
    public async Task RobotsTxt_HasCacheControlHeader()
    {
        var response = await _client.GetAsync("/robots.txt");
        response.EnsureSuccessStatusCode();

        // ResponseCache(Duration = 3600) should set Cache-Control
        response.Headers.CacheControl.Should().NotBeNull();
    }

    [Fact]
    public async Task SitemapXml_HasCacheControlHeader()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        response.EnsureSuccessStatusCode();

        response.Headers.CacheControl.Should().NotBeNull();
    }

    [Fact]
    public async Task FeedXml_HasCacheControlHeader()
    {
        var response = await _client.GetAsync("/feed.xml");
        response.EnsureSuccessStatusCode();

        response.Headers.CacheControl.Should().NotBeNull();
    }

    [Fact]
    public async Task AtomXml_HasCacheControlHeader()
    {
        var response = await _client.GetAsync("/atom.xml");
        response.EnsureSuccessStatusCode();

        response.Headers.CacheControl.Should().NotBeNull();
    }
}
