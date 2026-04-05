using Blog.Api.Common.Attributes;
using Blog.Api.Common.Models;
using System.Text;
using System.Text.Json;

namespace Blog.Api.Middleware;

/// <summary>
/// Wraps successful (2xx) JSON API responses in a uniform <see cref="ApiResponse{T}"/> envelope.
/// Endpoints annotated with <see cref="RawResponseAttribute"/> — or those returning non-JSON content
/// (e.g. file downloads, health checks) — are passed through unmodified.
/// </summary>
public class ResponseEnvelopeMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for [RawResponse] opt-out before executing the pipeline.
        // We defer the check until after routing has resolved the endpoint.
        var originalBody = context.Response.Body;

        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // Determine whether this endpoint opts out of envelope wrapping.
        var endpoint = context.GetEndpoint();
        var skipEnvelope = endpoint?.Metadata.GetMetadata<RawResponseAttribute>() != null
            || context.Request.Path.StartsWithSegments("/health");

        var status = context.Response.StatusCode;
        var isSuccess = status is >= 200 and <= 299;
        var isJson = context.Response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;

        if (!skipEnvelope && isSuccess && isJson && buffer.Length > 0)
        {
            // Read the buffered response payload.
            buffer.Seek(0, SeekOrigin.Begin);
            var originalJson = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

            // Deserialise the inner payload as a raw JSON element so we can re-embed it verbatim.
            JsonElement innerData;
            try
            {
                innerData = JsonSerializer.Deserialize<JsonElement>(originalJson);
            }
            catch
            {
                // If the body is not valid JSON (e.g. 204 with empty body), pass through as-is.
                buffer.Seek(0, SeekOrigin.Begin);
                await buffer.CopyToAsync(originalBody);
                return;
            }

            var envelope = new
            {
                data = innerData,
                timestamp = DateTime.UtcNow
            };

            var envelopeJson = JsonSerializer.Serialize(envelope, _jsonOptions);
            var envelopeBytes = Encoding.UTF8.GetBytes(envelopeJson);

            context.Response.ContentLength = envelopeBytes.Length;
            await originalBody.WriteAsync(envelopeBytes);
        }
        else
        {
            // Pass through unchanged (non-2xx, non-JSON, skipped endpoint, or empty body).
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody);
        }
    }
}
