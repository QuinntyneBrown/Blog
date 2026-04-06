using Blog.Api.Common.Models;
using Blog.Api.Features.Events.Commands;
using Blog.Api.Features.Events.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

public class EventsController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
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
        var result = await Mediator.Send(new GetPublishedEventsQuery(upcomingPage, pastPage, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetEventBySlugQuery(slug), ct);
        return Ok(result);
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
