using System.Text.RegularExpressions;

namespace Blog.Api.Middleware;

public partial class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 64;

    [GeneratedRegex(@"^[A-Za-z0-9\-_]+$")]
    private static partial Regex SafeCharsPattern();

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            && !string.IsNullOrEmpty(headerValue)
            && headerValue.ToString().Length <= MaxCorrelationIdLength
            && SafeCharsPattern().IsMatch(headerValue.ToString()))
        {
            correlationId = headerValue.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }
}
