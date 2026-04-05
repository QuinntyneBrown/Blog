using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected IActionResult PagedResult<T>(Blog.Api.Common.Models.PagedResponse<T> response) => Ok(response);
    protected IActionResult CreatedResource<T>(T resource, string routeName, object routeValues)
        => CreatedAtRoute(routeName, routeValues, resource);
}
