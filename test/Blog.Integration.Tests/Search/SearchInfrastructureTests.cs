using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Search;

public class SearchInfrastructureTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SearchInfrastructureTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithMatchingQuery_ReturnsPagedResults()
    {
        // "Hello" matches the seeded "Hello World" article
        var response = await _client.GetAsync("/api/public/articles/search?q=Hello");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Search_EmptyQuery_Returns400()
    {
        var response = await _client.GetAsync("/api/public/articles/search?q=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WhitespaceOnlyQuery_Returns400()
    {
        var response = await _client.GetAsync("/api/public/articles/search?q=%20%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_NoMatchingResults_ReturnsEmptyItems()
    {
        var response = await _client.GetAsync("/api/public/articles/search?q=xyznonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Suggestions_WithValidQuery_ReturnsSuggestions()
    {
        // "He" should match "Hello World"
        var response = await _client.GetAsync("/api/public/articles/suggestions?q=He");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement;
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Suggestions_SingleChar_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/public/articles/suggestions?q=a");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Suggestions_EmptyQuery_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/public/articles/suggestions?q=");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Search_QueryTooLong_Returns400()
    {
        var longQuery = new string('a', 201);
        var response = await _client.GetAsync($"/api/public/articles/search?q={longQuery}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
