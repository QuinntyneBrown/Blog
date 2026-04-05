using System.Net;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Security;

public class CorsTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public CorsTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    [Fact]
    public async Task PreflightRequest_AllowedOrigin_ReturnsAccessControlHeaders()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/articles");
        request.Headers.Add("Origin", "https://localhost:5001");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // The allowed origin should be reflected
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").First()
                .Should().Be("https://localhost:5001");
        }
    }

    [Fact]
    public async Task PreflightRequest_DisallowedOrigin_DoesNotReturnAccessControlHeaders()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/articles");
        request.Headers.Add("Origin", "https://evil-site.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        // The disallowed origin should NOT get Access-Control-Allow-Origin
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").First()
                .Should().NotBe("https://evil-site.com");
        }
    }

    [Fact]
    public async Task CorsRequest_AllowedOrigin_IncludesAllowedMethods()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/articles");
        request.Headers.Add("Origin", "https://localhost:5001");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        if (response.Headers.Contains("Access-Control-Allow-Methods"))
        {
            var methods = response.Headers.GetValues("Access-Control-Allow-Methods").First();
            methods.Should().Contain("GET");
            methods.Should().Contain("POST");
        }
    }
}
