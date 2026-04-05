using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Auth;

public class JwtMiddlewareTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public JwtMiddlewareTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    [Fact]
    public async Task GetArticles_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetArticles_WithValidToken_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetArticles_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-jwt-token");

        var response = await client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetArticles_WithExpiredToken_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        // A properly-formed but expired JWT will be rejected
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0IiwiZXhwIjoxMDAwMDAwMDAwfQ.invalid");

        var response = await client.GetAsync("/api/articles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
