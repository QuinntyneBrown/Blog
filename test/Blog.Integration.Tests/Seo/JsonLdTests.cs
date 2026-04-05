using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Seo;

public class JsonLdTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JsonLdTests(BlogWebApplicationFactory factory)
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
    public async Task GetArticleBySlug_ReturnsJsonLd_WithArticleType()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<script type=\"application/ld+json\">");
        html.Should().Contain("\"@type\": \"Article\"");
    }

    [Fact]
    public async Task GetArticleBySlug_JsonLdContainsHeadline()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("\"headline\":");
    }

    [Fact]
    public async Task GetArticleBySlug_JsonLdContainsDatePublished()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("\"datePublished\":");
    }

    [Fact]
    public async Task GetArticleBySlug_JsonLdContainsAuthor()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("\"author\":");
    }

    [Fact]
    public async Task GetArticleBySlug_JsonLdContainsDescription()
    {
        var response = await _client.GetAsync("/articles/hello-world");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("\"description\":");
    }
}
