using Blog.Api.Common.Models;
using Blog.Api.Features.Newsletters.Queries;
using Blog.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[Route("api/newsletters/archive")]
[ApiController]
public class PublicNewslettersController(IMediator mediator, IConfiguration configuration, INewsletterRepository newsletters) : ApiControllerBase(mediator, configuration)
{
    [HttpGet]
    public async Task<IActionResult> GetArchive(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize < 1 || pageSize > 50) return BadRequest("pageSize must be between 1 and 50");

        var result = await Mediator.Send(new GetNewsletterArchiveQuery(page, pageSize), ct);

        // Conditional GET support (design §6)
        if (result.TotalCount > 0)
        {
            var latestDateSent = await newsletters.GetLatestDateSentAsync(ct);
            if (latestDateSent.HasValue)
            {
                var lastModified = new DateTimeOffset(latestDateSent.Value, TimeSpan.Zero);
                Response.Headers.LastModified = lastModified.ToString("R");
                var epoch = latestDateSent.Value.Ticks / TimeSpan.TicksPerSecond;
                var etag = $"W/\"archive:{result.TotalCount}:{epoch}\"";
                Response.Headers.ETag = etag;

                // If-None-Match takes precedence over If-Modified-Since (RFC 7232 §6)
                if (Request.Headers.IfNoneMatch.Count > 0)
                {
                    if (Request.Headers.IfNoneMatch.ToString() == etag)
                        return StatusCode(304);
                }
                else if (Request.Headers.IfModifiedSince.Count > 0 &&
                         DateTimeOffset.TryParse(Request.Headers.IfModifiedSince.ToString(), out var ifModifiedSince) &&
                         lastModified <= ifModifiedSince)
                {
                    return StatusCode(304);
                }
            }
        }

        Response.Headers.CacheControl = "public, max-age=300, stale-while-revalidate=600";
        return PagedResult(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetNewsletterBySlugQuery(slug), ct);

        // Conditional GET support (design §6)
        if (result.DateSent.HasValue)
        {
            var lastModified = new DateTimeOffset(result.DateSent.Value, TimeSpan.Zero);
            Response.Headers.LastModified = lastModified.ToString("R");
            var etag = $"W/\"{result.Slug}\"";
            Response.Headers.ETag = etag;

            // If-None-Match takes precedence over If-Modified-Since (RFC 7232 §6)
            if (Request.Headers.IfNoneMatch.Count > 0)
            {
                if (Request.Headers.IfNoneMatch.ToString() == etag)
                    return StatusCode(304);
            }
            else if (Request.Headers.IfModifiedSince.Count > 0 &&
                     DateTimeOffset.TryParse(Request.Headers.IfModifiedSince.ToString(), out var ifModifiedSince) &&
                     lastModified <= ifModifiedSince)
            {
                return StatusCode(304);
            }
        }

        Response.Headers.CacheControl = "public, max-age=300, stale-while-revalidate=600";
        return Ok(result);
    }
}
