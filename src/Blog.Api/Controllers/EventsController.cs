using Blog.Api.Common.Models;
using Blog.Api.Features.Events.Commands;
using Blog.Api.Features.Events.Queries;
using Blog.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace Blog.Api.Controllers;

public class EventsController(IMediator mediator, IConfiguration configuration, IEventRepository eventRepository) : ApiControllerBase(mediator, configuration)
{
    private const string PublicCacheControl = "public, max-age=60, stale-while-revalidate=300";

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParameters paging, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetEventsQuery(paging.Page, paging.PageSize), ct);
        return PagedResult(result);
    }

    [HttpGet("{id:guid}", Name = "GetEventById")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetEventByIdQuery(id), ct);
        Response.Headers.ETag = $"W/\"event-{result.EventId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpGet("published")]
    public async Task<IActionResult> GetPublished(
        [FromQuery] int upcomingPage = 1,
        [FromQuery] int pastPage = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var stats = await eventRepository.GetPublishedStatsAsync(ct);
        var etag = $"W/\"pub:{stats.MaxVersion}:{stats.Count}\"";
        var truncatedUpdatedAt = new DateTime(
            stats.MaxUpdatedAt.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        var lastModified = new DateTimeOffset(truncatedUpdatedAt, TimeSpan.Zero);

        if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch == etag)
        {
            Response.Headers.CacheControl = PublicCacheControl;
            return StatusCode(304);
        }

        if (Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSince) &&
            DateTimeOffset.TryParse(ifModifiedSince, out var ifModifiedSinceDate) &&
            lastModified <= ifModifiedSinceDate)
        {
            Response.Headers.CacheControl = PublicCacheControl;
            return StatusCode(304);
        }

        var result = await Mediator.Send(new GetPublishedEventsQuery(upcomingPage, pastPage, pageSize), ct);

        Response.Headers.CacheControl = PublicCacheControl;
        Response.Headers.ETag = etag;
        Response.Headers[HeaderNames.LastModified] = lastModified.ToString("R");
        return Ok(result);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetEventBySlugQuery(slug), ct);

        var etag = $"W/\"{result.Event.Slug}:{result.Version}\"";
        var lastModified = new DateTimeOffset(
            new DateTime(result.UpdatedAt.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc),
            TimeSpan.Zero);

        Response.Headers.CacheControl = PublicCacheControl;
        Response.Headers.ETag = etag;
        Response.Headers[HeaderNames.LastModified] = lastModified.ToString("R");

        if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch == etag)
            return StatusCode(304);

        if (Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSince) &&
            DateTimeOffset.TryParse(ifModifiedSince, out var ifModifiedSinceDate) &&
            lastModified <= ifModifiedSinceDate)
            return StatusCode(304);

        return Ok(result.Event);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Create([FromBody] CreateEventCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = $"W/\"event-{result.EventId}-v{result.Version}\"";
        return CreatedResource(result, "GetEventById", new { id = result.EventId });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventBody body, CancellationToken ct)
    {
        var command = new UpdateEventCommand(
            id, body.Title, body.Description,
            body.StartDate, body.EndDate, body.TimeZoneId,
            body.Location, body.ExternalUrl, body.Version);
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = $"W/\"event-{result.EventId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteEventCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishUnpublishBody body, CancellationToken ct)
    {
        var result = await Mediator.Send(new PublishEventCommand(id, body.Version), ct);
        Response.Headers.ETag = $"W/\"event-{result.EventId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/unpublish")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Unpublish(Guid id, [FromBody] PublishUnpublishBody body, CancellationToken ct)
    {
        var result = await Mediator.Send(new UnpublishEventCommand(id, body.Version), ct);
        Response.Headers.ETag = $"W/\"event-{result.EventId}-v{result.Version}\"";
        return Ok(result);
    }
}

public record UpdateEventBody(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    string TimeZoneId,
    string Location,
    string? ExternalUrl,
    int Version);

public record PublishUnpublishBody(int Version);
