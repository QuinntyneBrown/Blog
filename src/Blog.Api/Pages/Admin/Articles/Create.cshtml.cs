using Blog.Api.Features.Articles.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Admin.Articles;

public class AdminArticleCreateModel(IMediator mediator) : AdminPageModelBase
{
    public void OnGet()
    {
        if (!IsAuthenticated()) Response.Redirect("/admin/login");
    }

    public async Task<IActionResult> OnPostAsync(string title, string body, [FromForm(Name = "abstract")] string articleAbstract, Guid? featuredImageId)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            var result = await mediator.Send(new CreateArticleCommand(title, body, articleAbstract, featuredImageId));
            return RedirectToPage("/Admin/Articles/Edit", new { id = result.ArticleId, success = "Article created." });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("title", ex.Message);
            return Page();
        }
    }
}