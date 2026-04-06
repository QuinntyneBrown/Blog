using System.Security.Cryptography;

namespace Blog.Api.Middleware;

/// <summary>
/// Injects security-related HTTP response headers on every response, including a
/// per-request nonce-based Content-Security-Policy as resolved in design OQ-1
/// (docs/detailed-designs/08-security-hardening/README.md).
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
{
    /// <summary>
    /// Key used to store the per-request CSP nonce in HttpContext.Items so that
    /// Razor Pages tag helpers can embed it into inline <style> blocks.
    /// </summary>
    public const string CspNonceKey = "CspNonce";

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a cryptographically-random per-request nonce (32 hex chars).
        // Hex encoding avoids HTML entity encoding issues that occur with base64's
        // +, /, = characters in nonce attributes.
        var nonceBytes = RandomNumberGenerator.GetBytes(16);
        var nonce = Convert.ToHexString(nonceBytes).ToLowerInvariant();

        // Store the nonce so Razor views/tag-helpers can embed it into <style> blocks.
        context.Items[CspNonceKey] = nonce;

        // Register a callback so headers are written just before the response body starts.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Content-Security-Policy — nonce-based; eliminates 'unsafe-inline' for styles.
            // fonts.googleapis.com is allowed in style-src so the Google Fonts CSS stylesheet can
            // be applied (loaded via <link rel="preload" onload="this.rel='stylesheet'">).
            // fonts.gstatic.com is allowed in font-src so the actual .woff2 font binary files
            // (referenced by the Google Fonts stylesheet) can be downloaded.
            // report-uri (legacy) and report-to (modern) directives send CSP violations
            // to the /api/csp-report endpoint (Design 08, Section 3.3).
            headers["Content-Security-Policy"] =
                $"default-src 'self'; " +
                $"script-src 'self' 'nonce-{nonce}'; " +
                $"style-src 'self' 'nonce-{nonce}' https://fonts.googleapis.com; " +
                $"font-src 'self' https://fonts.gstatic.com; " +
                $"img-src 'self' data:; " +
                $"frame-ancestors 'none'; " +
                $"object-src 'none'; " +
                $"base-uri 'self'; " +
                $"form-action 'self'; " +
                $"report-uri /api/csp-report; " +
                $"report-to csp-endpoint";

            // Reporting-Endpoints header (modern browsers) — maps the "csp-endpoint" group
            // to the /api/csp-report URL (Design 08, Section 3.3).
            headers["Reporting-Endpoints"] = "csp-endpoint=\"/api/csp-report\"";

            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // Remove the Server header to avoid information disclosure.
            headers.Remove("Server");

            return Task.CompletedTask;
        });

        await next(context);
    }
}
