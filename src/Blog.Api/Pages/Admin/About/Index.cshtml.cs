using Blog.Api.Features.About.Commands;
using Blog.Api.Features.About.Queries;
using Blog.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Pages.Admin.About;

public class AdminAboutIndexModel(IMediator mediator, IAboutContentRepository aboutContents) : AdminPageModelBase
{
    public AboutContentDto? About { get; private set; }
    public List<AboutContentHistoryDto> History { get; private set; } = new();
    public int HistoryTotalCount { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");

        var entity = await aboutContents.GetCurrentAsync();
        if (entity != null)
        {
            var imageUrl = entity.ProfileImage != null ? $"/assets/{entity.ProfileImage.StoredFileName}" : null;
            About = new AboutContentDto(
                entity.AboutContentId, entity.Heading, entity.Body, entity.BodyHtml,
                entity.ProfileImageId, imageUrl,
                entity.CreatedAt, entity.UpdatedAt, entity.Version);

            var historyResult = await mediator.Send(new GetAboutHistoryQuery(1, 20));
            History = historyResult.Items;
            HistoryTotalCount = historyResult.TotalCount;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string heading, string body, Guid? profileImageId, int version)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            var userId = GetCurrentUserId();
            await mediator.Send(new UpsertAboutContentCommand(heading, body, profileImageId, version, userId));
            return RedirectToPage("/Admin/About/Index", new { success = "About page saved." });
        }
        catch (Exception ex)
        {
            return RedirectToPage("/Admin/About/Index", new { error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostRestoreAsync(Guid historyId, int currentVersion)
    {
        if (!IsAuthenticated()) return RedirectToPage("/Admin/Login");
        try
        {
            var result = await mediator.Send(new RestoreAboutContentCommand(historyId, currentVersion));
            var msg = result.ProfileImageRestored
                ? "About page restored."
                : "About page restored. The profile image from this revision is no longer available.";
            return RedirectToPage("/Admin/About/Index", new { success = msg });
        }
        catch (Exception ex)
        {
            return RedirectToPage("/Admin/About/Index", new { error = ex.Message });
        }
    }
}
