using System.Text.Encodings.Web;
using Blog.Api.Common.Models;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Search;

public class SearchIndexModel(IMediator mediator) : PageModel
{
    public string Query { get; private set; } = string.Empty;
    public PagedResponse<SearchResultDto> Results { get; private set; } = new();
    public bool IsEmpty => Results.TotalCount == 0 && !string.IsNullOrWhiteSpace(Query);
    public int CurrentPage { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync(
        [FromQuery(Name = "q")] string? q,
        [FromQuery(Name = "page")] int page = 1)
    {
        // Empty query → render empty state immediately, no DB call
        if (string.IsNullOrWhiteSpace(q))
        {
            Query = string.Empty;
            return Page();
        }

        // Enforce max length per L2-053
        if (q.Length > 200)
            q = q[..200];

        Query = q.Trim();
        CurrentPage = Math.Max(1, page);

        Results = await mediator.Send(
            new SearchArticlesQuery(Query, CurrentPage, 10));

        // Set page title and cache control
        ViewData["Title"] = $"{HtmlEncoder.Default.Encode(Query)} — Search";
        Response.Headers.CacheControl = "no-store";

        return Page();
    }
}
