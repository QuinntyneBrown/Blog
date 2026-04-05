using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Seo;

public class RobotsTxtTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RobotsTxtTests(BlogWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRobotsTxt_Returns200()
    {
        var response = await _client.GetAsync("/robots.txt");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRobotsTxt_ReturnsTextPlain()
    {
        var response = await _client.GetAsync("/robots.txt");

        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowAdmin()
    {
        var response = await _client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Disallow: /admin");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowApi()
    {
        var response = await _client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Disallow: /api/");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowSearch()
    {
        var response = await _client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Disallow: /search");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsSitemapUrl()
    {
        var response = await _client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Sitemap:");
        body.Should().Contain("/sitemap.xml");
    }
}
