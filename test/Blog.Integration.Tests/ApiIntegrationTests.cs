using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests;

public class SeoIntegrationTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SeoIntegrationTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetArticles_ReturnsJsonLd_WithBlogType()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<script type=\"application/ld+json\">");
        html.Should().Contain("\"@type\": \"Blog\"");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowSearch()
    {
        var response = await _client.GetAsync("/robots.txt");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Disallow: /search");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowAdmin()
    {
        var response = await _client.GetAsync("/robots.txt");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Disallow: /admin");
    }

    [Fact]
    public async Task GetRobotsTxt_ContainsDisallowApi()
    {
        var response = await _client.GetAsync("/robots.txt");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Disallow: /api/");
    }

    [Fact]
    public async Task GetArticleBySlug_ReturnsJsonLd_WithArticleType()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<script type=\"application/ld+json\">");
        html.Should().Contain("\"@type\": \"Article\"");
    }

    [Fact]
    public async Task GetArticleBySlug_JsonLdContainsRequiredFields()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("\"headline\":");
        html.Should().Contain("\"datePublished\":");
        html.Should().Contain("\"author\":");
        html.Should().Contain("\"description\":");
    }
}

