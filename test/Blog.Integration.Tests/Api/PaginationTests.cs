using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Api;

public class PaginationTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public PaginationTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    [Fact]
    public async Task GetArticles_DefaultPagination_ReturnsFirstPage()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/articles");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetArticles_SpecificPage_ReturnsRequestedPage()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/articles?page=1&pageSize=1");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeLessOrEqualTo(1);
    }

    [Fact]
    public async Task GetArticles_ResponseContainsTotalPages()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/articles?page=1&pageSize=1");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("totalPages").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPublicArticles_DefaultPagination_ReturnsFirstPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/articles");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPublicArticles_HasPreviousAndNextPageFlags()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/articles?page=1&pageSize=100");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse();
        // With 1 published article and pageSize=100, there should be no next page
        doc.RootElement.GetProperty("hasNextPage").GetBoolean().Should().BeFalse();
    }
}
