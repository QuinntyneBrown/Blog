using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Articles;

public class PublishArticleTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public PublishArticleTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Fact]
    public async Task PublishArticle_ValidRequest_SetsPublishedTrue()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a draft article
        var createPayload = new
        {
            title = "Publish Test " + Guid.NewGuid().ToString("N")[..8],
            body = "# Publish\n\nContent.",
            @abstract = "Publish abstract."
        };
        var createResponse = await client.PostAsync("/api/articles", JsonBody(createPayload));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var articleId = createDoc.RootElement.GetProperty("data").GetProperty("articleId").GetString();
        var etag = createResponse.Headers.ETag?.ToString();

        // Publish it
        var publishRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/articles/{articleId}/publish")
        {
            Content = JsonBody(new { published = true })
        };
        publishRequest.Headers.IfMatch.ParseAdd(etag!);
        var publishResponse = await client.SendAsync(publishRequest);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishBody = await publishResponse.Content.ReadAsStringAsync();
        using var publishDoc = JsonDocument.Parse(publishBody);
        publishDoc.RootElement.GetProperty("data").GetProperty("published").GetBoolean().Should().BeTrue();
        publishDoc.RootElement.GetProperty("data").GetProperty("datePublished").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnpublishArticle_ValidRequest_SetsPublishedFalse()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create and publish
        var createPayload = new
        {
            title = "Unpublish Test " + Guid.NewGuid().ToString("N")[..8],
            body = "# Unpublish\n\nContent.",
            @abstract = "Unpublish abstract."
        };
        var createResponse = await client.PostAsync("/api/articles", JsonBody(createPayload));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var articleId = createDoc.RootElement.GetProperty("data").GetProperty("articleId").GetString();
        var etag = createResponse.Headers.ETag?.ToString();

        // Publish
        var publishReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/articles/{articleId}/publish")
        {
            Content = JsonBody(new { published = true })
        };
        publishReq.Headers.IfMatch.ParseAdd(etag!);
        var publishResp = await client.SendAsync(publishReq);
        publishResp.EnsureSuccessStatusCode();
        var pubEtag = publishResp.Headers.ETag?.ToString();

        // Unpublish
        var unpublishReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/articles/{articleId}/publish")
        {
            Content = JsonBody(new { published = false })
        };
        unpublishReq.Headers.IfMatch.ParseAdd(pubEtag!);
        var unpublishResp = await client.SendAsync(unpublishReq);

        unpublishResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var unpubBody = await unpublishResp.Content.ReadAsStringAsync();
        using var unpubDoc = JsonDocument.Parse(unpubBody);
        unpubDoc.RootElement.GetProperty("data").GetProperty("published").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PublishArticle_NonExistentId_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PatchAsync(
            $"/api/articles/{Guid.NewGuid()}/publish",
            JsonBody(new { published = true }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublishArticle_StaleETag_Returns412()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create
        var createPayload = new
        {
            title = "Stale Publish " + Guid.NewGuid().ToString("N")[..8],
            body = "# Stale\n\nContent.",
            @abstract = "Stale abstract."
        };
        var createResponse = await client.PostAsync("/api/articles", JsonBody(createPayload));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var articleId = createDoc.RootElement.GetProperty("data").GetProperty("articleId").GetString();

        // Publish with stale ETag
        var publishReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/articles/{articleId}/publish")
        {
            Content = JsonBody(new { published = true })
        };
        publishReq.Headers.IfMatch.ParseAdd("W/\"article-stale-v999\"");
        var publishResp = await client.SendAsync(publishReq);

        publishResp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }
}
