using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Observability;

public class HealthCheckTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(BlogWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Health_ReturnsOk_WithHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task Health_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthReady_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_DoesNotRequireAuthentication()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
