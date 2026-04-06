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

        // Conditional GET support
        if (result.TotalCount > 0)
        {
            var latestDateSent = await newsletters.GetLatestDateSentAsync(ct);
            if (latestDateSent.HasValue)
            {
                var lastModified = new DateTimeOffset(latestDateSent.Value, TimeSpan.Zero);
                Response.Headers.LastModified = lastModified.ToString("R");
                var epoch = latestDateSent.Value.Ticks / TimeSpan.TicksPerSecond;
                Response.Headers.ETag = $"W/\"archive:{result.TotalCount}:{epoch}\"";

                if (Request.Headers.IfNoneMatch.Count > 0 &&
                    Request.Headers.IfNoneMatch.ToString() == $"W/\"archive:{result.TotalCount}:{epoch}\"")
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

        // Conditional GET support
        if (result.DateSent.HasValue)
        {
            var lastModified = new DateTimeOffset(result.DateSent.Value, TimeSpan.Zero);
            Response.Headers.LastModified = lastModified.ToString("R");
            Response.Headers.ETag = $"W/\"{result.Slug}\"";

            if (Request.Headers.IfNoneMatch.Count > 0 &&
                Request.Headers.IfNoneMatch.ToString() == $"W/\"{result.Slug}\"")
            {
                return StatusCode(304);
            }
        }

        Response.Headers.CacheControl = "public, max-age=300, stale-while-revalidate=600";
        return Ok(result);
    }
}
