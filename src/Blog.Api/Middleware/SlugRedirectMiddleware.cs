namespace Blog.Api.Middleware;

public class SlugRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (path != null && path.StartsWith("/articles/", StringComparison.OrdinalIgnoreCase) && path.Length > "/articles/".Length)
        {
            var slug = path["/articles/".Length..];
            var needsRedirect = false;
            var correctedSlug = slug;

            // Strip trailing slash
            if (correctedSlug.EndsWith('/'))
            {
                correctedSlug = correctedSlug.TrimEnd('/');
                needsRedirect = true;
            }

            // Lowercase check
            if (correctedSlug != correctedSlug.ToLowerInvariant())
            {
                correctedSlug = correctedSlug.ToLowerInvariant();
                needsRedirect = true;
            }

            if (needsRedirect && correctedSlug.Length > 0)
            {
                var query = context.Request.QueryString;
                context.Response.StatusCode = 301;
                context.Response.Headers.Location = $"/articles/{correctedSlug}{query}";
                return;
            }

            // Design spec (Section 3.7): file-extension and numeric-ID slug patterns are
            // not valid and must be rejected with 404 before reaching the routing layer.
            // This prevents bots probing .html/.php/.asp URLs from hitting the database
            // and keeps the clean-URL contract unambiguous.

            // Reject slugs that contain a dot — these carry a file extension (e.g. "my-post.html").
            if (correctedSlug.Contains('.'))
            {
                context.Response.StatusCode = 404;
                return;
            }

            // Reject slugs that consist entirely of decimal digits — these are numeric IDs
            // (e.g. "12345"). Valid slugs always contain at least one letter or hyphen.
            if (correctedSlug.Length > 0 && correctedSlug.All(char.IsDigit))
            {
                context.Response.StatusCode = 404;
                return;
            }
        }

        await next(context);
    }
}
