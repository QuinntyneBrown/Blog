using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Api;

public class ErrorHandlingTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public ErrorHandlingTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Fact]
    public async Task NotFound_ReturnsProblemDetailsFormat()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/articles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
        doc.RootElement.TryGetProperty("detail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ValidationError_ReturnsProblemDetailsWithErrors()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var payload = new { title = "", body = "", @abstract = "" };
        var response = await client.PostAsync("/api/articles", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.EnumerateObject().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Unauthorized_ReturnsProblemDetails()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync("/api/auth/login",
            JsonBody(new { email = "nobody@example.com", password = "WrongPass1234" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task ConflictError_Returns409WithProblemDetails()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create article with same title as seeded "Hello World"
        var payload = new { title = "Hello World", body = "# Body\n\nContent.", @abstract = "Abstract." };
        var response = await client.PostAsync("/api/articles", JsonBody(payload));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Conflict");
    }

    [Fact]
    public async Task ProblemDetails_IncludesInstancePath()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var id = Guid.NewGuid();

        var response = await client.GetAsync($"/api/articles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("instance").GetString().Should().Contain($"/api/articles/{id}");
    }
}
