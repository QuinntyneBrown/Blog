using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.PublicArticles;

public class ArticleListingPageTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ArticleListingPageTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetArticles_ReturnsHtml_WithArticleElements()
    {
        var response = await _client.GetAsync("/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<article");
    }

    [Fact]
    public async Task GetArticles_ShowsPublishedArticleTitles()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Hello World");
    }

    [Fact]
    public async Task GetArticles_DoesNotShowDraftArticles()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        // The seeded draft article should not appear in public listing
        html.Should().NotContain("Draft Article");
    }

    [Fact]
    public async Task GetPublicArticlesApi_ReturnsPaginatedResults()
    {
        var response = await _client.GetAsync("/api/public/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        doc.RootElement.GetProperty("data").GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("data").GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPublicArticlesApi_OnlyReturnsPublishedArticles()
    {
        var response = await _client.GetAsync("/api/public/articles");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("data").GetProperty("items");
        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("published").GetBoolean().Should().BeTrue();
        }
    }
}
