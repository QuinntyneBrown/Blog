using Blog.Api.Middleware;
using Blog.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Blog.Api.Tests.Middleware;

public class JwtMiddlewareTests
{
    private readonly ITokenService _mockTokenService;
    private readonly RequestDelegate _mockNext;
    private readonly JwtMiddleware _middleware;

    public JwtMiddlewareTests()
    {
        _mockTokenService = Substitute.For<ITokenService>();
        _mockNext = Substitute.For<RequestDelegate>();
        _middleware = new JwtMiddleware(_mockNext);
    }

    [Fact]
    public async Task InvokeAsync_WithValidBearerToken_SetsHttpContextUser()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "valid.jwt.token";
        var expectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Email, "admin@blog.com")
        }, "jwt"));

        context.Request.Headers.Authorization = $"Bearer {token}";
        _mockTokenService.ValidateToken(token).Returns(expectedPrincipal);

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().BeSameAs(expectedPrincipal);
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidBearerToken_DoesNotSetUser()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "invalid.jwt.token";

        context.Request.Headers.Authorization = $"Bearer {token}";
        _mockTokenService.ValidateToken(token).Returns((ClaimsPrincipal?)null);

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().NotBeNull();
        context.User.Identity?.IsAuthenticated.Should().BeFalse();
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_WithValidSessionToken_SetsHttpContextUser()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "session.jwt.token";
        var expectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-456"),
            new Claim(ClaimTypes.Email, "admin@blog.com")
        }, "jwt"));

        // Setup session
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        context.Session.SetString("jwt_token", token);

        _mockTokenService.ValidateToken(token).Returns(expectedPrincipal);

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().BeSameAs(expectedPrincipal);
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidSessionToken_DoesNotSetUser()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "invalid.session.token";

        // Setup session
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        context.Session.SetString("jwt_token", token);

        _mockTokenService.ValidateToken(token).Returns((ClaimsPrincipal?)null);

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().NotBeNull();
        context.User.Identity?.IsAuthenticated.Should().BeFalse();
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_BearerTokenTakesPriorityOverSession()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var bearerToken = "bearer.jwt.token";
        var sessionToken = "session.jwt.token";
        var bearerPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "bearer-user"),
            new Claim(ClaimTypes.Email, "bearer@blog.com")
        }, "jwt"));

        // Setup both bearer and session
        context.Request.Headers.Authorization = $"Bearer {bearerToken}";
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        context.Session.SetString("jwt_token", sessionToken);

        _mockTokenService.ValidateToken(bearerToken).Returns(bearerPrincipal);

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().BeSameAs(bearerPrincipal);
        _mockTokenService.Received(1).ValidateToken(bearerToken);
        _mockTokenService.DidNotReceive().ValidateToken(sessionToken);
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_WithoutAnyToken_DoesNotSetUser()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().NotBeNull();
        context.User.Identity?.IsAuthenticated.Should().BeFalse();
        _mockTokenService.DidNotReceive().ValidateToken(Arg.Any<string>());
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptySessionToken_DoesNotCallValidate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        
        // Setup session with empty token
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        context.Session.SetString("jwt_token", "");

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        _mockTokenService.DidNotReceive().ValidateToken(Arg.Any<string>());
        await _mockNext.Received(1).Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_CaseInsensitiveBearerPrefix()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var token = "test.jwt.token";
        var expectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-789")
        }, "jwt"));

        context.Request.Headers.Authorization = $"bearer {token}";  // lowercase 'bearer'
        _mockTokenService.ValidateToken(token).Returns(expectedPrincipal);

        // Act
        await _middleware.InvokeAsync(context, _mockTokenService);

        // Assert
        context.User.Should().BeSameAs(expectedPrincipal);
        await _mockNext.Received(1).Invoke(context);
    }
}

/// <summary>
/// Helper class to enable session support in DefaultHttpContext for tests
/// </summary>
internal class SessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = new TestSession();
}

/// <summary>
/// Simple in-memory session implementation for testing
/// </summary>
internal class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public string Id => "test-session-id";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        value = Array.Empty<byte>();
        return false;
    }
}
