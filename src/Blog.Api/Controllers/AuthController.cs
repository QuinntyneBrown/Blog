using Blog.Api.Common.Models;
using Blog.Api.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

public class AuthController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(result);
    }
}
