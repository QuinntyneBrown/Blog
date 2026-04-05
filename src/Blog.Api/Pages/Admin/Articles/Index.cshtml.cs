using Blog.Api.Common.Models;
using Blog.Api.Features.Articles.Commands;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Admin.Articles;

public class AdminArticlesIndexModel(IMediator mediator) : AdminPageModelBase
{
    public PagedResponse<ArticleListDto> Articles { get; private set; } = new();
    public int CurrentPage { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        Console.Error.WriteLine($"[INDEX] IsAuthenticated={IsAuthenticated()}");
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        CurrentPage = page;
        Articles = await mediator.Send(new GetArticlesQuery(page, 20));
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, int version)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            var ifMatch = $"W/\"article-{id}-v{version}\"";
            await mediator.Send(new DeleteArticleCommand(id, ifMatch));
        }
        catch { }
        return RedirectToPage("/Admin/Articles/Index");
    }
}
