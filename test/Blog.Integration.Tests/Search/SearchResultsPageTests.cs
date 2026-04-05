using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Search;

public class SearchResultsPageTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SearchResultsPageTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SearchPage_WithMatchingQuery_ReturnsHtmlWithResults()
    {
        var response = await _client.GetAsync("/search?q=Hello");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // Should contain result cards for matching articles
        html.Should().Contain("Hello World");
    }

    [Fact]
    public async Task SearchPage_WithNonMatchingQuery_ReturnsEmptyState()
    {
        var response = await _client.GetAsync("/search?q=noresultsexpected");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // Should render the page (not 404), even with no results
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SearchPage_WithNoQuery_ReturnsPage()
    {
        var response = await _client.GetAsync("/search");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SearchPage_QueryPreservedInHtml()
    {
        var response = await _client.GetAsync("/search?q=Hello");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        // The search query should be reflected in the page (e.g. in an input or heading)
        html.Should().Contain("Hello");
    }
}
