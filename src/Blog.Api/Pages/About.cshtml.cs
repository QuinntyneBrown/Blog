using Blog.Api.Features.About.Queries;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages;

[ResponseCache(CacheProfileName = "HtmlPage")]
public class AboutModel(IETagGenerator eTagGenerator, IAboutContentRepository aboutContents) : PageModel
{
    public PublicAboutContentDto? About { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var entity = await aboutContents.GetCurrentAsync();
            if (entity == null)
            {
                About = null;
                Response.Headers.CacheControl = "no-store";
                return Page();
            }

            var etag = eTagGenerator.GenerateAbout(entity.Version);
            var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
            if (eTagGenerator.IsMatch(etag, ifNoneMatch))
                return StatusCode(304);

            var imageUrl = entity.ProfileImage != null ? $"/assets/{entity.ProfileImage.StoredFileName}" : null;
            About = new PublicAboutContentDto(entity.Heading, entity.BodyHtml, imageUrl);

            Response.Headers.ETag = etag;
            Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
            Response.Headers.Append("Last-Modified", entity.UpdatedAt.ToUniversalTime().ToString("R"));
            return Page();
        }
        catch (Exception)
        {
            Response.StatusCode = 500;
            return Page();
        }
    }
}
