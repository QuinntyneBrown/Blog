using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blog.Api.Common.Models;
using Blog.Api.Features.About.Commands;
using Blog.Api.Features.About.Queries;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

public class AboutController(
    IMediator mediator,
    IConfiguration configuration,
    IETagGenerator eTagGenerator,
    IAboutContentRepository aboutContents) : ApiControllerBase(mediator, configuration)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var entity = await aboutContents.GetCurrentAsync(ct);
        if (entity == null)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(null as object);
        }

        var etag = eTagGenerator.GenerateAbout(entity.Version);
        var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
        if (eTagGenerator.IsMatch(etag, ifNoneMatch))
            return StatusCode(304);

        var ifModifiedSince = Request.Headers.IfModifiedSince.FirstOrDefault();
        if (!string.IsNullOrEmpty(ifModifiedSince)
            && DateTimeOffset.TryParse(ifModifiedSince, out var clientDate)
            && entity.UpdatedAt.ToUniversalTime() <= clientDate.UtcDateTime)
        {
            return StatusCode(304);
        }

        var imageUrl = entity.ProfileImage != null ? $"/assets/{entity.ProfileImage.StoredFileName}" : null;
        var dto = new PublicAboutContentDto(entity.Heading, entity.BodyHtml, imageUrl);

        Response.Headers.ETag = etag;
        Response.Headers.Append("Last-Modified", entity.UpdatedAt.ToUniversalTime().ToString("R"));
        Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
        return Ok(dto);
    }

    [HttpPut]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Upsert([FromBody] UpsertAboutContentBody body, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var command = new UpsertAboutContentCommand(body.Heading, body.Body, body.ProfileImageId, body.Version, userId);
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

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException());
    }
}

public record UpsertAboutContentBody(string Heading, string Body, Guid? ProfileImageId, int Version);
public record RestoreAboutContentBody(int CurrentVersion);
