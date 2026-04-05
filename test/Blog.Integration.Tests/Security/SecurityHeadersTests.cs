using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Security;

public class SecurityHeadersTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HtmlResponse_ContainsXContentTypeOptions()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    }

    [Fact]
    public async Task HtmlResponse_ContainsXFrameOptions()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task HtmlResponse_ContainsContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        csp.Should().Contain("nonce-");
        csp.Should().Contain("default-src 'self'");
    }

    [Fact]
    public async Task HtmlResponse_ContainsReferrerPolicy()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        response.Headers.GetValues("Referrer-Policy").First()
            .Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task HtmlResponse_ContainsPermissionsPolicy()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var policy = response.Headers.GetValues("Permissions-Policy").First();
        policy.Should().Contain("camera=()");
        policy.Should().Contain("microphone=()");
    }

    [Fact]
    public async Task ApiResponse_ContainsSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task Response_DoesNotContainServerHeader()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        response.Headers.Contains("Server").Should().BeFalse();
    }

    [Fact]
    public async Task CspNonce_IsDifferentPerRequest()
    {
        var response1 = await _client.GetAsync("/articles");
        var response2 = await _client.GetAsync("/articles");

        var csp1 = response1.Headers.GetValues("Content-Security-Policy").First();
        var csp2 = response2.Headers.GetValues("Content-Security-Policy").First();

        // Each request should have a unique nonce
        csp1.Should().NotBe(csp2);
    }
}
