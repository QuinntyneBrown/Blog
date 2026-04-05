using Blog.Api.Features.Articles.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Admin.Articles;

public class AdminArticleCreateModel(IMediator mediator) : PageModel
{
    public void OnGet()
    {
        if (!IsAuthenticated()) Response.Redirect("/admin/login");
    }

    public async Task<IActionResult> OnPostAsync(string title, string body, [FromForm(Name = "abstract")] string articleAbstract)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            var result = await mediator.Send(new CreateArticleCommand(title, body, articleAbstract, null));
            return RedirectToPage("/Admin/Articles/Edit", new { id = result.ArticleId, success = "Article created." });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("title", ex.Message);
            return Page();
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