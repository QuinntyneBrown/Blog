using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Articles;

public class ArticleCrudTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public ArticleCrudTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Fact]
    public async Task FullCrudCycle_CreateReadUpdateDelete_ReturnsCorrectCodes()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create
        var createPayload = new
        {
            title = "Integration Test Article " + Guid.NewGuid().ToString("N")[..8],
            body = "# Test\n\nThis is a test article body.",
            @abstract = "Test abstract for the article."
        };
        var createResponse = await client.PostAsync("/api/articles", JsonBody(createPayload));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var articleId = createDoc.RootElement.GetProperty("articleId").GetString();
        var version = createDoc.RootElement.GetProperty("version").GetInt32();
        articleId.Should().NotBeNullOrWhiteSpace();

        // Read
        var getResponse = await client.GetAsync($"/api/articles/{articleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = getResponse.Headers.ETag?.Tag;
        etag.Should().NotBeNullOrWhiteSpace();

        // Update
        var updatePayload = new
        {
            title = createPayload.title,
            body = "# Updated\n\nUpdated body content.",
            @abstract = "Updated abstract."
        };
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/articles/{articleId}")
        {
            Content = JsonBody(updatePayload)
        };
        updateRequest.Headers.IfMatch.ParseAdd(etag!);
        var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete
        var deleteEtag = updateResponse.Headers.ETag?.Tag;
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/articles/{articleId}");
        deleteRequest.Headers.IfMatch.ParseAdd(deleteEtag!);
        var deleteResponse = await client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getAfterDelete = await client.GetAsync($"/api/articles/{articleId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateArticle_DuplicateSlug_Returns409()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var payload = new
        {
            title = "Hello World",  // Matches seeded article slug
            body = "# Duplicate\n\nBody.",
            @abstract = "Duplicate abstract."
        };
        var response = await client.PostAsync("/api/articles", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateArticle_StaleETag_Returns412()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create an article
        var createPayload = new
        {
            title = "ETag Test " + Guid.NewGuid().ToString("N")[..8],
            body = "# ETag\n\nBody.",
            @abstract = "ETag abstract."
        };
        var createResponse = await client.PostAsync("/api/articles", JsonBody(createPayload));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(createBody);
        var articleId = doc.RootElement.GetProperty("articleId").GetString();

        // Try to update with a stale ETag
        var updatePayload = new
        {
            title = "Updated Title",
            body = "Updated body.",
            @abstract = "Updated abstract."
        };
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/articles/{articleId}")
        {
            Content = JsonBody(updatePayload)
        };
        updateRequest.Headers.IfMatch.ParseAdd("W/\"article-stale-v999\"");
        var updateResponse = await client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task CreateArticle_MissingTitle_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var payload = new
        {
            title = "",
            body = "# Body\n\nContent.",
            @abstract = "Abstract."
        };
        var response = await client.PostAsync("/api/articles", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateArticle_MissingBody_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var payload = new
        {
            title = "Valid Title",
            body = "",
            @abstract = "Abstract."
        };
        var response = await client.PostAsync("/api/articles", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateArticle_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var payload = new
        {
            title = "Unauth Article",
            body = "# Body\n\nContent.",
            @abstract = "Abstract."
        };
        var response = await client.PostAsync("/api/articles", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
