using Blog.Api.Features.About.Commands;
using Blog.Api.Features.About.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AboutController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAboutContentQuery(), ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Upsert([FromBody] UpsertAboutContentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}
