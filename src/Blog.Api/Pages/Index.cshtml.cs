using Blog.Api.Common.Models;
using Blog.Api.Features.Articles.Queries;
using Blog.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages;

[ResponseCache(CacheProfileName = "HtmlPage")]
public class IndexModel(IMediator mediator, IETagGenerator eTagGenerator) : PageModel
{
    public PagedResponse<ArticleListDto> Articles { get; private set; } = new();
    public int CurrentPage { get; private set; } = 1;

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = page;
        Articles = await mediator.Send(new GetPublishedArticlesQuery(page, 9));

        // Generate ETag from the page content hash so reverse proxies and browsers
        // can use conditional GET requests.
        var hashInput = string.Join(",", Articles.Items.Select(a => $"{a.ArticleId}:{a.Version}"));
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"index-p{page}-{hashInput}"));
        var etag = $"W/\"{Convert.ToHexString(hash[..8]).ToLowerInvariant()}\"";
        var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
        if (eTagGenerator.IsMatch(etag, ifNoneMatch))
        {
            Response.StatusCode = 304;
            return;
        }
        Response.Headers.ETag = etag;
        Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
    }
}
