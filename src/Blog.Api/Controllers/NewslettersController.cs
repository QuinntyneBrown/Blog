using Blog.Api.Common.Models;
using Blog.Api.Features.Newsletters.Commands;
using Blog.Api.Features.Newsletters.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

public class NewslettersController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize < 1 || pageSize > 50) return BadRequest("pageSize must be between 1 and 50");

        var result = await Mediator.Send(new GetNewslettersQuery(page, pageSize, status), ct);
        return PagedResult(result);
    }

    [HttpGet("{id:guid}", Name = "GetNewsletterById")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetNewsletterByIdQuery(id), ct);
        Response.Headers.ETag = $"W/\"newsletter-{result.NewsletterId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Create([FromBody] CreateNewsletterCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = $"W/\"newsletter-{result.NewsletterId}-v{result.Version}\"";
        return CreatedResource(result, "GetNewsletterById", new { id = result.NewsletterId });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNewsletterBody body, CancellationToken ct)
    {
        var command = new UpdateNewsletterCommand(id, body.Subject, body.Body, body.Version);
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = $"W/\"newsletter-{result.NewsletterId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteNewsletterCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new SendNewsletterCommand(id), ct);
        return Accepted();
    }
}

public record UpdateNewsletterBody(string Subject, string Body, int Version);
