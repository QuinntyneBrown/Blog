using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Auth.Commands;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Blog.Api.Tests.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IEmailRateLimitService _emailRateLimitService = Substitute.For<IEmailRateLimitService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<AuthService> _logger = Substitute.For<ILogger<AuthService>>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepository,
            _passwordHasher,
            _tokenService,
            _emailRateLimitService,
            _uow,
            _logger);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponseWithTokenAndFutureExpiresAt()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            PasswordHash = "hashed",
            DisplayName = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        int retryAfter;
        _emailRateLimitService.TryAcquire("admin@blog.dev", out retryAfter).Returns(true);
        _userRepository.GetByEmailAsync("admin@blog.dev", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("Admin1234!", "hashed").Returns(true);
        _tokenService.GenerateToken(user).Returns("jwt-token-here");
        _tokenService.GetExpiration().Returns(DateTime.UtcNow.AddMinutes(60));

        // Act
        var result = await _sut.LoginAsync("admin@blog.dev", "Admin1234!", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            PasswordHash = "hashed",
            DisplayName = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        int retryAfter;
        _emailRateLimitService.TryAcquire("admin@blog.dev", out retryAfter).Returns(true);
        _userRepository.GetByEmailAsync("admin@blog.dev", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("WrongPassword", "hashed").Returns(false);

        // Act & Assert
        var act = () => _sut.LoginAsync("admin@blog.dev", "WrongPassword", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        int retryAfter;
        _emailRateLimitService.TryAcquire("nobody@example.com", out retryAfter).Returns(true);
        _userRepository.GetByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act & Assert
        var act = () => _sut.LoginAsync("nobody@example.com", "SomePassword", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_UpdatesLastLoginAt()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            PasswordHash = "hashed",
            DisplayName = "Admin",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        int retryAfter;
        _emailRateLimitService.TryAcquire("admin@blog.dev", out retryAfter).Returns(true);
        _userRepository.GetByEmailAsync("admin@blog.dev", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyPassword("Admin1234!", "hashed").Returns(true);
        _tokenService.GenerateToken(user).Returns("jwt-token");
        _tokenService.GetExpiration().Returns(DateTime.UtcNow.AddMinutes(60));

        // Act
        await _sut.LoginAsync("admin@blog.dev", "Admin1234!", CancellationToken.None);

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _userRepository.Received(1).Update(user);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_RateLimitExceeded_ThrowsRateLimitExceededException()
    {
        // Arrange
        _emailRateLimitService.TryAcquire("admin@blog.dev", out Arg.Any<int>())
            .Returns(x =>
            {
                x[1] = 300;
                return false;
            });

        // Act & Assert
        var act = () => _sut.LoginAsync("admin@blog.dev", "Admin1234!", CancellationToken.None);
        await act.Should().ThrowAsync<RateLimitExceededException>();
    }
}
