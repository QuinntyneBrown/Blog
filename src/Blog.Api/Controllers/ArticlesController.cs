using Blog.Api.Common.Models;
using Blog.Api.Features.Articles.Commands;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Blog.Api.Controllers;

public class ArticlesController(IMediator mediator, IConfiguration configuration) : ApiControllerBase(mediator, configuration)
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParameters paging, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetArticlesQuery(paging.Page, paging.PageSize), ct);
        return PagedResult(result);
    }

    [HttpGet("{id:guid}", Name = "GetArticleById")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetArticleByIdQuery(id), ct);
        Response.Headers.ETag = $"W/\"article-{result.ArticleId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Create([FromBody] CreateArticleCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = $"W/\"article-{result.ArticleId}-v{result.Version}\"";
        return CreatedResource(result, "GetArticleById", new { id = result.ArticleId });
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleCommand body, CancellationToken ct)
    {
        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        var command = body with { Id = id, IfMatch = ifMatch };
        var result = await Mediator.Send(command, ct);
        Response.Headers.ETag = $"W/\"article-{result.ArticleId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpPatch("{id:guid}/publish")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishArticleBody body, CancellationToken ct)
    {
        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        var result = await Mediator.Send(new PublishArticleCommand(id, body.Published, ifMatch), ct);
        Response.Headers.ETag = $"W/\"article-{result.ArticleId}-v{result.Version}\"";
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [EnableRateLimiting("write-endpoints")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        await Mediator.Send(new DeleteArticleCommand(id, ifMatch), ct);
        return NoContent();
    }
}

public record PublishArticleBody(bool Published);
