using Blog.Api.Common.Models;
using Blog.Api.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

public class AuthController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
    [HttpPost("login")]
    [EnableRateLimiting("login-ip")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(result);
    }
}
