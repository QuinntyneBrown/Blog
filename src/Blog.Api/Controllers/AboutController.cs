using Blog.Api.Common.Models;
using Blog.Api.Features.About.Commands;
using Blog.Api.Features.About.Queries;
using Blog.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

public class AboutController(IMediator mediator, IConfiguration configuration, IETagGenerator eTagGenerator) : ApiControllerBase(mediator, configuration)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var about = await Mediator.Send(new GetAboutContentQuery(), ct);
        if (about == null)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(null as object);
        }

        // We need the version for ETag — re-fetch from repo via a separate internal mechanism
        // For simplicity, the controller works with the public DTO only; ETag is set at the Razor page level
        Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
        return Ok(about);
    }

    [HttpPut]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Upsert([FromBody] UpsertAboutContentCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = eTagGenerator.GenerateAbout(result.Version);
        return Ok(result);
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetHistory([FromQuery] PaginationParameters paging, CancellationToken ct)
    {
        var pageSize = paging.PageSize > 50 ? 50 : paging.PageSize;
        var result = await Mediator.Send(new GetAboutHistoryQuery(paging.Page, pageSize), ct);
        return PagedResult(result);
    }

    [HttpPut("restore/{historyId:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Restore(Guid historyId, [FromBody] RestoreAboutContentBody body, CancellationToken ct)
    {
        var result = await Mediator.Send(new RestoreAboutContentCommand(historyId, body.CurrentVersion), ct);
        Response.Headers.ETag = eTagGenerator.GenerateAbout(result.Version);
        return Ok(result);
    }
}

public record RestoreAboutContentBody(int CurrentVersion);
