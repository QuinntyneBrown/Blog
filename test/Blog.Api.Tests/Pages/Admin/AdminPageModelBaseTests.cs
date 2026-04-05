using Blog.Api.Pages.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Xunit;

namespace Blog.Api.Tests.Pages.Admin;

public class AdminPageModelBaseTests
{
    private readonly TestableAdminPageModel _pageModel;

    public AdminPageModelBaseTests()
    {
        _pageModel = new TestableAdminPageModel();
    }

    [Fact]
    public void IsAuthenticated_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var context = CreateHttpContext();
        SetupSession(context, "valid.jwt.token", DateTime.UtcNow.AddMinutes(30));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicIsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var context = CreateHttpContext();
        SetupSession(context, "expired.jwt.token", DateTime.UtcNow.AddMinutes(-10));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicIsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithNoToken_ReturnsFalse()
    {
        // Arrange
        var context = CreateHttpContext();
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicIsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithMissingExpiration_ReturnsFalse()
    {
        // Arrange
        var context = CreateHttpContext();
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        context.Session.SetString("jwt_token", "valid.jwt.token");
        // Don't set jwt_expires
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicIsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithInvalidExpirationFormat_ReturnsFalse()
    {
        // Arrange
        var context = CreateHttpContext();
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        context.Session.SetString("jwt_token", "valid.jwt.token");
        context.Session.SetString("jwt_expires", "not-a-valid-date");
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicIsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetCurrentUserId_WithValidSubClaim_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString())
        }, "jwt"));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithValidNameIdentifierClaim_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "jwt"));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithBothClaims_PrefersNameIdentifier()
    {
        // Arrange
        var nameIdentifierUserId = Guid.NewGuid();
        var subUserId = Guid.NewGuid();
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, nameIdentifierUserId.ToString()),
            new Claim("sub", subUserId.ToString())
        }, "jwt"));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserId();

        // Assert
        result.Should().Be(nameIdentifierUserId);
    }

    [Fact]
    public void GetCurrentUserId_WithNoClaims_ReturnsEmpty()
    {
        // Arrange
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserId();

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetCurrentUserEmail_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var email = "admin@blog.com";
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email)
        }, "jwt"));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserEmail();

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void GetCurrentUserEmail_WithNoEmailClaim_ReturnsNull()
    {
        // Arrange
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserEmail();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserDisplayName_WithDisplayNameClaim_ReturnsDisplayName()
    {
        // Arrange
        var displayName = "Admin User";
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("displayName", displayName)
        }, "jwt"));
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserDisplayName();

        // Assert
        result.Should().Be(displayName);
    }

    [Fact]
    public void GetCurrentUserDisplayName_WithNoDisplayNameClaim_ReturnsNull()
    {
        // Arrange
        var context = CreateHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = _pageModel.PublicGetCurrentUserDisplayName();

        // Assert
        result.Should().BeNull();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        // Setup session feature by default
        var sessionFeature = new SessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        return context;
    }

    private static void SetupSession(HttpContext context, string token, DateTime expiresAt)
    {
        context.Session.SetString("jwt_token", token);
        context.Session.SetString("jwt_expires", expiresAt.ToString("O"));
    }

    private static PageContext CreatePageContext(HttpContext httpContext)
    {
        return new PageContext(new ActionContext(httpContext, new RouteData(), new CompiledPageActionDescriptor()));
    }

    /// <summary>
    /// Testable wrapper that exposes protected methods for testing
    /// </summary>
    private class TestableAdminPageModel : AdminPageModelBase
    {
        public bool PublicIsAuthenticated() => IsAuthenticated();
        public Guid PublicGetCurrentUserId() => GetCurrentUserId();
        public string? PublicGetCurrentUserEmail() => GetCurrentUserEmail();
        public string? PublicGetCurrentUserDisplayName() => GetCurrentUserDisplayName();
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
