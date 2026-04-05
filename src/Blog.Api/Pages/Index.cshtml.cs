using Blog.Api.Common.Models;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public PagedResponse<ArticleListDto> Articles { get; private set; } = new();
    public int CurrentPage { get; private set; } = 1;

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = page;
        Articles = await mediator.Send(new GetPublishedArticlesQuery(page, 9));
    }
}
