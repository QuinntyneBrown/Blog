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
        }

        await next(context);
    }
}
