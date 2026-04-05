using Blog.Api.Common.Models;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[Route("api/public/articles")]
[ApiController]
public class PublicArticlesController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParameters paging, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublishedArticlesQuery(paging.Page, paging.PageSize), ct);
        return PagedResult(result);
    }

    [HttpGet("{slug}", Name = "GetPublishedArticleBySlug")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublishedArticleBySlugQuery(slug), ct);
        return Ok(result);
    }
}
