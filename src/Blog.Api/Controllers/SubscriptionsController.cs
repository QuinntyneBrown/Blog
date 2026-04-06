using Blog.Api.Common.Models;
using Blog.Api.Features.Subscriptions.Commands;
using Blog.Api.Features.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

[Route("api/newsletter-subscriptions")]
public class SubscriptionsController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
    [HttpPost]
    [EnableRateLimiting("newsletter-subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Accepted();
    }

    [HttpPost("confirm")]
    [EnableRateLimiting("newsletter-confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmSubscriptionCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok();
    }

    [HttpDelete("{token}")]
    [EnableRateLimiting("newsletter-unsubscribe")]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken ct)
    {
        await Mediator.Send(new UnsubscribeCommand(token), ct);
        return NoContent();
    }

    [HttpPost("unsubscribe")]
    [EnableRateLimiting("newsletter-unsubscribe")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> OneClickUnsubscribe(
        [FromQuery] string token,
        CancellationToken ct)
    {
        // RFC 8058: body should contain List-Unsubscribe=One-Click
        // Read form data to get the "List-Unsubscribe" field (hyphenated key can't be a C# param)
        var form = await Request.ReadFormAsync(ct);
        var listUnsubscribe = form["List-Unsubscribe"].FirstOrDefault();
        var body = $"List-Unsubscribe={listUnsubscribe}";
        await Mediator.Send(new OneClickUnsubscribeCommand(token ?? string.Empty, body), ct);
        return NoContent();
    }

    // GET /api/subscribers — paginated subscriber list (auth-guarded, for back office).
    // The route lives on SubscriptionsController per design §3.2 because the subscriber
    // lifecycle (subscribe, confirm, unsubscribe, list) is cohesive within this controller.
    [HttpGet("/api/subscribers")]
    [Authorize]
    public async Task<IActionResult> GetSubscribers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        if (page < 1) return BadRequest("page must be >= 1");
        if (pageSize < 1 || pageSize > 50) return BadRequest("pageSize must be between 1 and 50");

        var result = await Mediator.Send(new GetSubscribersQuery(page, pageSize, status), ct);
        return PagedResult(result);
    }
}
