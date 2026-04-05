using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Blog.Api.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Admin;

public class LoginModel(IMediator mediator) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string ErrorMessage { get; private set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        try
        {
            var result = await mediator.Send(new LoginCommand(email, password));
            HttpContext.Session.SetString("jwt_token", result.Token);
            HttpContext.Session.SetString("jwt_expires", result.ExpiresAt.ToString("O"));
            return RedirectToPage("/Admin/Articles/Index");
        }
        catch
        {
            Email = email;
            ErrorMessage = "Invalid email or password. Please try again.";
            return Page();
        }
    }
}