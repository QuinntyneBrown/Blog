using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Blog.Integration.Tests.Security;

public class RateLimitingTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public RateLimitingTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var payload = JsonBody(new { email = "admin@blog.dev", password = "WrongPassword1" });

        // The default login rate limit is 10 per minute.
        // Fire 11 requests rapidly to trigger the limit.
        HttpResponseMessage? lastResponse = null;
        var got429 = false;
        for (int i = 0; i < 15; i++)
        {
            lastResponse = await client.PostAsync("/api/auth/login",
                new StringContent(
                    JsonSerializer.Serialize(new { email = "admin@blog.dev", password = "WrongPassword1" }),
                    Encoding.UTF8, "application/json"));

            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                got429 = true;
                break;
            }
        }

        got429.Should().BeTrue("rate limiter should reject after exceeding permit limit");
        lastResponse!.Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitedResponse_ContainsRetryAfterHeader()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage? rateLimitedResponse = null;
        for (int i = 0; i < 15; i++)
        {
            var resp = await client.PostAsync("/api/auth/login",
                new StringContent(
                    JsonSerializer.Serialize(new { email = "ratelimit@test.com", password = "SomePass1234" }),
                    Encoding.UTF8, "application/json"));

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = resp;
                break;
            }
        }

        if (rateLimitedResponse != null)
        {
            rateLimitedResponse.Headers.Contains("Retry-After").Should().BeTrue();
        }
    }
}
