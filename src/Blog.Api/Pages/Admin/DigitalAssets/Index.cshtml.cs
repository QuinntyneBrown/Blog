using Blog.Api.Common.Exceptions;
using Blog.Api.Features.DigitalAssets.Commands;
using Blog.Api.Features.DigitalAssets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Blog.Api.Pages.Admin.DigitalAssets;

public class AdminDigitalAssetsIndexModel(IMediator mediator) : AdminPageModelBase
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
        try
        {
            await mediator.Send(new DeleteDigitalAssetCommand(id));
        }
        catch (Exception ex) when (ex is ConflictException or NotFoundException)
        {
            return RedirectToPage(new { error = ex.Message });
        }
        catch
        {
            return RedirectToPage(new { error = "An error occurred while deleting the asset." });
        }
        return RedirectToPage(new { success = "Asset deleted." });
    }


}