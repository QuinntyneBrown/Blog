using Blog.Api.Services;
using Microsoft.AspNetCore.Http.Features;

namespace Blog.Api.Middleware;

/// <summary>
/// Extracts and validates JWT tokens from either:
/// 1. The <c>Authorization: Bearer</c> request header (for API requests), or
/// 2. The session-stored "jwt_token" value (for authenticated Razor Pages).
///
/// Populates <see cref="HttpContext.User"/> with the resulting
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> on successful validation.
///
/// Implements the Token Validation Flow described in
/// docs/detailed-designs/01-authentication/README.md, Section 3.5 and Section 5.2.
///
/// If the token is missing or invalid the request proceeds without an authenticated identity;
/// the <c>[Authorize]</c> attribute on protected controllers/pages then returns 401 Unauthorized.
/// </summary>
public class JwtMiddleware(RequestDelegate next)
{
    private const string BearerPrefix = "Bearer ";
    private const string SessionTokenKey = "jwt_token";

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        string? token = null;

        // Priority 1: Check Authorization header (for API requests with bearer tokens)
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader[BearerPrefix.Length..].Trim();
        }
        // Priority 2: Check session-stored JWT (for authenticated Razor Pages)
        else
        {
            var sessionFeature = context.Features.Get<ISessionFeature>();
            if (sessionFeature?.Session != null)
            {
                token = sessionFeature.Session.GetString(SessionTokenKey);
            }
        }

        // Validate token and set HttpContext.User if valid
        if (!string.IsNullOrEmpty(token))
        {
            var principal = tokenService.ValidateToken(token);
            if (principal is not null)
            {
                context.User = principal;
            }
        }

        await next(context);
    }
}
