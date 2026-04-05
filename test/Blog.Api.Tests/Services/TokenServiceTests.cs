using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blog.Api.Services;
using Blog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Blog.Api.Tests.Services;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "BlogTestIssuer",
            ["Jwt:Audience"] = "BlogTestAudience",
            ["Jwt:ExpirationMinutes"] = "30"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        _sut = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_ProducesJwtWithCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            DisplayName = "Test Admin",
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _sut.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value
            .Should().Be(user.UserId.ToString());
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value
            .Should().Be("admin@blog.dev");
        jwt.Issuer.Should().Be("BlogTestIssuer");
        jwt.Audiences.Should().Contain("BlogTestAudience");
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ExpirationMatchesConfiguration()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            DisplayName = "Test Admin",
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Should expire approximately 30 minutes from now (within 2 minutes tolerance)
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            DisplayName = "Test Admin",
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow
        };
        var token = _sut.GenerateToken(user);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value
            .Should().Be(user.UserId.ToString());
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            DisplayName = "Test Admin",
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow
        };
        var token = _sut.GenerateToken(user);
        // Tamper with the token by modifying the payload
        var parts = token.Split('.');
        parts[1] = parts[1] + "tampered";
        var tamperedToken = string.Join('.', parts);

        // Act
        var principal = _sut.ValidateToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange — create a token service with negative expiration to produce an already-expired token
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "BlogTestIssuer",
            ["Jwt:Audience"] = "BlogTestAudience",
            ["Jwt:ExpirationMinutes"] = "-1"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        var expiredTokenService = new TokenService(config);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            DisplayName = "Test Admin",
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow
        };
        var token = expiredTokenService.GenerateToken(user);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GetExpiration_ReturnsConfiguredMinutesFromNow()
    {
        // Act
        var expiration = _sut.GetExpiration();

        // Assert
        expiration.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetExpiration_DefaultsTo60Minutes_WhenNotConfigured()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "BlogTestIssuer",
            ["Jwt:Audience"] = "BlogTestAudience"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        var service = new TokenService(config);

        // Act
        var expiration = service.GetExpiration();

        // Assert
        expiration.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }
}
