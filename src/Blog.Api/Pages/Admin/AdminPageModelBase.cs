using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Blog.Api.Pages.Admin;

/// <summary>
/// Base class for admin Razor Pages that require authentication.
/// Provides consistent authentication state checks and user identity extraction.
/// </summary>
public abstract class AdminPageModelBase : PageModel
{
    /// <summary>
    /// Checks if the current session has a valid JWT token.
    /// </summary>
    protected bool IsAuthenticated()
    {
        var token = HttpContext.Session.GetString("jwt_token");
        var expires = HttpContext.Session.GetString("jwt_expires");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expires)) return false;
        if (!DateTime.TryParse(expires, null, System.Globalization.DateTimeStyles.RoundtripKind, out var exp)) return false;
        return exp.ToUniversalTime() > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the current user ID from the JWT claims populated by JwtMiddleware.
    /// Returns Guid.Empty if the user is not authenticated or the claim is missing.
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        // The JwtMiddleware validates the session JWT and populates HttpContext.User with claims
        // Extract the user ID from the "sub" claim (JwtRegisteredClaimNames.Sub)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        
        return string.IsNullOrEmpty(userId) ? Guid.Empty : Guid.Parse(userId);
    }

    /// <summary>
    /// Gets the current user's email from the JWT claims.
    /// Returns null if the user is not authenticated or the claim is missing.
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
    }

    /// <summary>
    /// Gets the current user's display name from the JWT claims.
    /// Returns null if the user is not authenticated or the claim is missing.
    /// </summary>
    protected string? GetCurrentUserDisplayName()
    {
        return User.FindFirstValue("displayName");
    }
}
