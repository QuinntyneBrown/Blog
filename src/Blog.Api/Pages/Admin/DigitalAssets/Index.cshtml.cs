using Blog.Api.Features.DigitalAssets.Commands;
using Blog.Api.Features.DigitalAssets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Blog.Api.Pages.Admin.DigitalAssets;

public class AdminDigitalAssetsIndexModel(IMediator mediator) : PageModel
{
    public List<DigitalAssetDto> Assets { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        var userId = GetCurrentUserId();
        Assets = await mediator.Send(new GetDigitalAssetsQuery(userId));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return RedirectToPage("/Admin/Login");
        try
        {
            await mediator.Send(new UploadDigitalAssetCommand(file, userId));
        }
        catch (Exception ex)
        {
            return RedirectToPage(new { error = ex.Message });
        }
        return RedirectToPage(new { success = "Asset uploaded." });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try { await mediator.Send(new DeleteDigitalAssetCommand(id)); }
        catch { }
        return RedirectToPage(new { success = "Asset deleted." });
    }

    private bool IsAuthenticated()
    {
        var token = HttpContext.Session.GetString("jwt_token");
        var expires = HttpContext.Session.GetString("jwt_expires");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expires)) return false;
        if (!DateTime.TryParse(expires, out var exp)) return false;
        return exp > DateTime.UtcNow;
    }

    private Guid GetCurrentUserId()
    {
        // For demo: use a placeholder. Real implementation reads from session/claims.
        return Guid.Empty;
    }
}