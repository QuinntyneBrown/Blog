using Blog.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Blog.Api.Tests.Services;

public class EmailRateLimitServiceTests
{
    private readonly EmailRateLimitService _sut;

    public EmailRateLimitServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["RateLimiting:EmailLoginMaxAttempts"] = "5"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        _sut = new EmailRateLimitService(config);
    }

    [Fact]
    public void TryAcquire_AllowsUpTo5Attempts()
    {
        var email = "test@example.com";

        for (int i = 1; i <= 5; i++)
        {
            var allowed = _sut.TryAcquire(email, out var retryAfter);
            allowed.Should().BeTrue($"attempt {i} should be allowed");
            retryAfter.Should().Be(0);
        }
    }

    [Fact]
    public void TryAcquire_BlocksSixthAttemptWithRetryAfter()
    {
        var email = "blocked@example.com";

        // Use up all 5 attempts
        for (int i = 0; i < 5; i++)
        {
            _sut.TryAcquire(email, out _).Should().BeTrue();
        }

        // 6th should be blocked
        var allowed = _sut.TryAcquire(email, out var retryAfterSeconds);
        allowed.Should().BeFalse();
        retryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TryAcquire_IsCaseInsensitive()
    {
        // Use up quota with mixed casing
        for (int i = 0; i < 5; i++)
        {
            _sut.TryAcquire("USER@Example.COM", out _);
        }

        // 6th should be blocked regardless of casing
        var allowed = _sut.TryAcquire("user@example.com", out _);
        allowed.Should().BeFalse();
    }

    [Fact]
    public void TryAcquire_DifferentEmails_IndependentLimits()
    {
        // Fill up one email
        for (int i = 0; i < 5; i++)
        {
            _sut.TryAcquire("user1@example.com", out _);
        }

        // A different email should still be allowed
        var allowed = _sut.TryAcquire("user2@example.com", out _);
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryAcquire_DefaultsTo5WhenNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var sut = new EmailRateLimitService(config);

        var email = "default@example.com";
        for (int i = 0; i < 5; i++)
        {
            sut.TryAcquire(email, out _).Should().BeTrue();
        }

        sut.TryAcquire(email, out _).Should().BeFalse();
    }
}
