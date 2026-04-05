using Blog.Api.Common.Models;
using Blog.Api.Features.DigitalAssets.Commands;
using Blog.Api.Features.DigitalAssets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Blog.Api.Controllers;

[Route("api/digital-assets")]
public class DigitalAssetsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());
        var result = await Mediator.Send(new GetDigitalAssetsQuery(userId), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetDigitalAssetById")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalAssetByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());
        var result = await Mediator.Send(new UploadDigitalAssetCommand(file, userId), ct);
        return CreatedResource(result, "GetDigitalAssetById", new { id = result.DigitalAssetId });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteDigitalAssetCommand(id), ct);
        return NoContent();
    }
}
