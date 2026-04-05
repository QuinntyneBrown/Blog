using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Blog.Integration.Tests;

public class HealthEndpointTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(BlogWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Health_ReturnsOk_WithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task HealthReady_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act — no Authorization header sent
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class CorrelationIdTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorrelationIdTests(BlogWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AnyRequest_IncludesCorrelationIdHeader()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-ID");
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItBack()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", "my-trace-id");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.GetValues("X-Correlation-ID").First().Should().Be("my-trace-id");
    }

    [Fact]
    public async Task Request_WithMaliciousCorrelationId_GeneratesNewId()
    {
        // Arrange — XSS payload should be rejected by the regex filter
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", "<script>alert(1)</script>");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
        correlationId.Should().NotBe("<script>alert(1)</script>");
        // Should be a valid GUID since the middleware generates one
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }
}
