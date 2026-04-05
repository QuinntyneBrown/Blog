using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public new int StatusCode { get; set; } = 500;
    public void OnGet(int? statusCode)
    {
        StatusCode = statusCode ?? 500;
    }
}
