using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

/// <summary>
/// Receives Content-Security-Policy violation reports from browsers.
/// Design reference: docs/detailed-designs/08-security-hardening/README.md, Section 3.3.
/// </summary>
[ApiController]
[Route("api/csp-report")]
[AllowAnonymous]
[EnableRateLimiting("csp-report")]
public class CspReportController(ILogger<CspReportController> logger) : ControllerBase
{
    /// <summary>
    /// Accepts CSP violation reports sent by browsers via the report-uri / report-to directives.
    /// Logs the violation and returns 204 No Content.
    /// </summary>
    [HttpPost]
    [Consumes("application/csp-report", "application/json", "application/reports+json")]
    public async Task<IActionResult> Report(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        logger.LogWarning("CSP violation report received: {CspReport}", body);

        return NoContent();
    }
}
