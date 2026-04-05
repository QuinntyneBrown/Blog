namespace Blog.Api.Common.Attributes;

/// <summary>
/// Marks an endpoint or controller so that <see cref="Blog.Api.Middleware.ResponseEnvelopeMiddleware"/>
/// skips wrapping the response in an <c>ApiResponse&lt;T&gt;</c> envelope.
/// Apply to file-serving endpoints, health checks, and any other endpoint that must return a raw response body.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RawResponseAttribute : Attribute { }
