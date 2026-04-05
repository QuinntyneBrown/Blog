using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Seo;

public class FeedTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FeedTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRssFeed_Returns200()
    {
        var response = await _client.GetAsync("/feed.xml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRssFeed_ReturnsRssContentType()
    {
        var response = await _client.GetAsync("/feed.xml");

        response.Content.Headers.ContentType!.MediaType.Should().Contain("rss");
    }

    [Fact]
    public async Task GetRssFeed_ContainsChannelAndItems()
    {
        var response = await _client.GetAsync("/feed.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("<channel>");
        body.Should().Contain("<item>");
    }

    [Fact]
    public async Task GetRssFeed_ContainsArticleData()
    {
        var response = await _client.GetAsync("/feed.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Hello World");
        body.Should().Contain("/articles/hello-world");
    }

    [Fact]
    public async Task GetAtomFeed_Returns200()
    {
        var response = await _client.GetAsync("/atom.xml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAtomFeed_ReturnsAtomContentType()
    {
        var response = await _client.GetAsync("/atom.xml");

        response.Content.Headers.ContentType!.MediaType.Should().Contain("atom");
    }

    [Fact]
    public async Task GetAtomFeed_ContainsFeedAndEntries()
    {
        var response = await _client.GetAsync("/atom.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("feed");
        body.Should().Contain("entry");
    }

    [Fact]
    public async Task GetAtomFeed_ContainsArticleData()
    {
        var response = await _client.GetAsync("/atom.xml");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Hello World");
        body.Should().Contain("/articles/hello-world");
    }

    [Fact]
    public async Task GetLlmsTxt_Returns200WithTextPlain()
    {
        var response = await _client.GetAsync("/llms.txt");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetLlmsTxt_ContainsArticleListing()
    {
        var response = await _client.GetAsync("/llms.txt");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Hello World");
        body.Should().Contain("/articles/hello-world");
    }

    [Fact]
    public async Task GetJsonFeed_Returns200()
    {
        var response = await _client.GetAsync("/feed/json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJsonFeed_ContainsArticleItems()
    {
        var response = await _client.GetAsync("/feed/json");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Hello World");
        body.Should().Contain("hello-world");
    }
}
