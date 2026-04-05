using Blog.Api.Services;

namespace Blog.Api.Middleware;

/// <summary>
/// Extracts the JWT bearer token from the <c>Authorization</c> request header, validates it
/// via <see cref="ITokenService.ValidateToken"/>, and populates <see cref="HttpContext.User"/>
/// with the resulting <see cref="System.Security.Claims.ClaimsPrincipal"/> on success.
///
/// Implements the Token Validation Flow described in
/// docs/detailed-designs/01-authentication/README.md, Section 3.5 and Section 5.2.
///
/// If the token is missing or invalid the request proceeds without an authenticated identity;
/// the <c>[Authorize]</c> attribute on protected controllers then returns 401 Unauthorized.
/// </summary>
public class JwtMiddleware(RequestDelegate next, ITokenService tokenService)
{
    private const string BearerPrefix = "Bearer ";

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader[BearerPrefix.Length..].Trim();

            var principal = tokenService.ValidateToken(token);
            if (principal is not null)
            {
                context.User = principal;
            }
        }

        await next(context);
    }
}
