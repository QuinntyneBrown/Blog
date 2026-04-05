using System.Diagnostics;

namespace Blog.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        await next(context);

        sw.Stop();

        var statusCode = context.Response.StatusCode;
        var durationMs = sw.ElapsedMilliseconds;
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var correlationId = context.Items["X-Correlation-ID"]?.ToString() ?? string.Empty;
        var timestamp = DateTimeOffset.UtcNow;

        const string template =
            "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms | CorrelationId: {CorrelationId} | Timestamp: {Timestamp}";

        if (statusCode >= 500)
        {
            logger.LogError(template, method, path, statusCode, durationMs, correlationId, timestamp);
        }
        else if (statusCode >= 400)
        {
            logger.LogWarning(template, method, path, statusCode, durationMs, correlationId, timestamp);
        }
        else
        {
            logger.LogInformation(template, method, path, statusCode, durationMs, correlationId, timestamp);
        }
    }
}
