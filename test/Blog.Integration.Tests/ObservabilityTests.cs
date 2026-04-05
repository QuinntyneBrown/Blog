using System.Net;
using System.Text.Json;
using FluentAssertions;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Blog.Integration.Tests;

/// <summary>
/// Custom factory that replaces the SQL Server database with an in-memory provider
/// and removes hosted services (MigrationRunner, SeedDataHostedService) that require
/// a real database.
/// </summary>
public class BlogWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the real SQL Server DbContext registration.
            services.RemoveAll<DbContextOptions<BlogDbContext>>();
            services.RemoveAll<BlogDbContext>();

            // Remove hosted services that depend on a real database.
            services.RemoveAll<IHostedService>();

            // Add BlogDbContext backed by an in-memory database.
            services.AddDbContextPool<BlogDbContext>(options =>
                options.UseInMemoryDatabase("BlogTestDb_" + Guid.NewGuid().ToString("N")));
        });
    }
}

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
