using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Articles.Commands;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Admin.Articles;

public class AdminArticleEditModel(IMediator mediator) : PageModel
{
    public ArticleDto? Article { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try { Article = await mediator.Send(new GetArticleByIdQuery(id)); }
        catch (NotFoundException) { return NotFound(); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string title, string body, [FromForm(Name = "abstract")] string articleAbstract, string action, int version, Guid? featuredImageId, IFormFile? featuredImage)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        var ifMatch = $"W/\"article-{id}-v{version}\"";
        try
        {
            var updated = await mediator.Send(new UpdateArticleCommand(id, title, body, articleAbstract, featuredImageId, ifMatch));
            if (action == "publish")
            {
                var newIfMatch = $"W/\"article-{updated.ArticleId}-v{updated.Version}\"";
                await mediator.Send(new PublishArticleCommand(id, !updated.Published, newIfMatch));
            }
            return RedirectToPage("/Admin/Articles/Edit", new { id, success = "Article saved." });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Admin/Articles/Edit", new { id, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, int version)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            var ifMatch = $"W/\"article-{id}-v{version}\"";
            await mediator.Send(new DeleteArticleCommand(id, ifMatch));
            return RedirectToPage("/Admin/Articles/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Admin/Articles/Edit", new { id });
        }
    }

    private bool IsAuthenticated()
    {
        var token = HttpContext.Session.GetString("jwt_token");
        var expires = HttpContext.Session.GetString("jwt_expires");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expires)) return false;
        if (!DateTime.TryParse(expires, out var exp)) return false;
        return exp > DateTime.UtcNow;
    }
}