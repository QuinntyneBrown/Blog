using Blog.Api.Services;

namespace Blog.Api.Middleware;

/// <summary>
/// Rewrites incoming requests for content-hashed static files
/// (e.g., /css/app.a1b2c3d4.css) back to their original paths
/// (e.g., /css/app.css) so the static file middleware can serve them.
/// </summary>
public class ContentHashRewriteMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IContentHashService _hashService;

    public ContentHashRewriteMiddleware(RequestDelegate next, IContentHashService hashService)
    {
        _next = next;
        _hashService = hashService;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path is not null)
        {
            var resolved = _hashService.ResolveHashedPath(path);
            if (resolved is not null)
            {
                context.Request.Path = resolved;
            }
        }

        return _next(context);
    }
}
