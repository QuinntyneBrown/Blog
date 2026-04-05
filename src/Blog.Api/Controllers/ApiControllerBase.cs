using Blog.Api.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Blog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    /// <summary>
    /// Returns a 200 OK result with pagination navigation URLs populated on the response.
    /// URLs are built from the configured <c>Site:SiteUrl</c> base to avoid host-header injection
    /// (Feature 06, Section 3.3 — PaginationHelper).
    /// </summary>
    protected IActionResult PagedResult<T>(PagedResponse<T> response)
    {
        var siteUrl = configuration["Site:SiteUrl"] ?? string.Empty;
        PaginationHelper.SetNavigationUrls(response, siteUrl, Request.Path.Value ?? string.Empty);
        return Ok(response);
    }

    protected IActionResult CreatedResource<T>(T resource, string routeName, object routeValues)
        => CreatedAtRoute(routeName, routeValues, resource);
}
