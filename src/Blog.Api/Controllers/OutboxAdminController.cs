using Blog.Api.Features.Outbox.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[Route("api/admin/outbox")]
[ApiController]
[Authorize]
public class OutboxAdminController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
    [HttpPost("{id:guid}/replay")]
    public async Task<IActionResult> Replay(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new ReplayOutboxMessageCommand(id), ct);
        return NoContent();
    }

    [HttpPost("replay")]
    public async Task<IActionResult> BulkReplay([FromQuery] string? messageType, CancellationToken ct)
    {
        var count = await Mediator.Send(new BulkReplayOutboxMessagesCommand(messageType), ct);
        return Ok(new { replayedCount = count });
    }
}
