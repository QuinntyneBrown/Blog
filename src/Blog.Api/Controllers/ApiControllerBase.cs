using Blog.Api.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected IActionResult PagedResult<T>(PagedResponse<T> response) => Ok(ApiResponse<PagedResponse<T>>.Ok(response));
    protected IActionResult CreatedResource<T>(T resource, string routeName, object routeValues)
        => CreatedAtRoute(routeName, routeValues, ApiResponse<T>.Ok(resource));
}
