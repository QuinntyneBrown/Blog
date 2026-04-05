using Blog.Api.Features.About.Commands;
using Blog.Api.Features.About.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Pages.Admin.About;

public class AdminAboutEditModel(IMediator mediator) : AdminPageModelBase
{
    public AboutContentDto? AboutContent { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        AboutContent = await mediator.Send(new GetAboutContentQuery());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string heading, string body, Guid? profileImageId)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            await mediator.Send(new UpsertAboutContentCommand(heading, body, profileImageId));
            return RedirectToPage("/Admin/About/Index", new { success = "About content saved." });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("heading", ex.Message);
            AboutContent = await mediator.Send(new GetAboutContentQuery());
            return Page();
        }
    }
}
