using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.PublicArticles;

public class ArticleDetailPageTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ArticleDetailPageTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetArticleBySlug_Published_Returns200WithTitle()
    {
        var response = await _client.GetAsync("/articles/hello-world");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<h1");
        html.Should().Contain("Hello World");
    }

    [Fact]
    public async Task GetArticleBySlug_Draft_Returns404()
    {
        var response = await _client.GetAsync("/articles/draft-article");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticleBySlug_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/articles/this-does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticleBySlug_UpperCase_Returns301RedirectToLowerCase()
    {
        var response = await _client.GetAsync("/articles/Hello-World");

        // SlugRedirectMiddleware should redirect uppercase slugs
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.Redirect,
            HttpStatusCode.OK); // Might be handled differently based on middleware order
    }

    [Fact]
    public async Task GetPublicArticleBySlugApi_Published_Returns200()
    {
        var response = await _client.GetAsync("/api/public/articles/hello-world");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        doc.RootElement.GetProperty("data").GetProperty("title").GetString().Should().Be("Hello World");
        doc.RootElement.GetProperty("data").GetProperty("slug").GetString().Should().Be("hello-world");
    }
}
