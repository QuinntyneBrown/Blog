using System.Collections.Concurrent;

namespace Blog.Api.Middleware;

/// <summary>
/// Simple in-memory rate limiter for login attempts, keyed by client IP.
/// Uses a fixed window of 1 minute with a configurable permit limit.
/// </summary>
public class LoginRateLimitMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly int _permitLimit = configuration.GetValue("RateLimiting:LoginPermitLimit", 10);

    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _counters = new();

    public async Task InvokeAsync(HttpContext context)
    {
        var isLoginPost = context.Request.Path.StartsWithSegments("/api/auth/login")
            && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);

        if (isLoginPost)
        {
            Console.WriteLine($"[RATELIMIT] Login attempt from {context.Connection.RemoteIpAddress}, path={context.Request.Path}, count={_counters.GetValueOrDefault(context.Connection.RemoteIpAddress?.ToString() ?? "unknown").Count}, limit={_permitLimit}");
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            var entry = _counters.AddOrUpdate(ip,
                _ => (1, now),
                (_, existing) =>
                {
                    if ((now - existing.WindowStart).TotalMinutes >= 1)
                        return (1, now); // Reset window
                    return (existing.Count + 1, existing.WindowStart);
                });

            Console.WriteLine($"[RATELIMIT] After update: count={entry.Count}, limit={_permitLimit}, will reject={entry.Count > _permitLimit}");
            if (entry.Count > _permitLimit)
            {
                Console.WriteLine("[RATELIMIT] REJECTING");
                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = "60";
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    "{\"type\":\"https://tools.ietf.org/html/rfc6585#section-4\",\"title\":\"Too Many Requests\",\"status\":429,\"detail\":\"Rate limit exceeded. Please try again later.\"}");
                return;
            }
        }

        await next(context);
    }
}
