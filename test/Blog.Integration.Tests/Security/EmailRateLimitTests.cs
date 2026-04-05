using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Blog.Integration.Tests.Security;

public class EmailRateLimitTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;

    public EmailRateLimitTests(BlogWebApplicationFactory factory)
    {
        factory.EnsureSeeded();
        _factory = factory;
    }

    [Fact]
    public async Task EmailRateLimit_ExceedingMaxAttempts_Returns429()
    {
        // Use a unique email to avoid cross-test interference
        var email = $"emaillimit-{Guid.NewGuid():N}@test.com";
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // The default email login max attempts in dev is 10000, but the IP-based rate
        // limiter (configured at 10 in non-dev) will trigger first. This test verifies
        // that the IP-based rate limiter returns 429 with Retry-After.
        var got429 = false;
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 15; i++)
        {
            lastResponse = await client.PostAsync("/api/auth/login",
                new StringContent(
                    JsonSerializer.Serialize(new { email, password = "WrongPassword1!" }),
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
}
