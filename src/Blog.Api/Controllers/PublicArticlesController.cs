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

    // GET /api/public/articles/search?q=azure&page=1
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q is required");
        if (q.Length > 200) return BadRequest("q must not exceed 200 characters");
        var result = await Mediator.Send(new SearchArticlesQuery(q.Trim(), page, 10), ct);
        return PagedResult(result);
    }

    // GET /api/public/articles/suggestions?q=azure
    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions(
        [FromQuery] string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Ok(Array.Empty<object>());
        if (q.Length > 200) return BadRequest("q must not exceed 200 characters");
        var result = await Mediator.Send(new GetSearchSuggestionsQuery(q.Trim()), ct);
        return Ok(result);
    }
}
