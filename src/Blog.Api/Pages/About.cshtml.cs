using Blog.Api.Features.About.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages;

[ResponseCache(CacheProfileName = "HtmlPage")]
public class AboutModel(IMediator mediator) : PageModel
{
    public AboutContentDto? AboutContent { get; private set; }

    public async Task OnGetAsync()
    {
        AboutContent = await mediator.Send(new GetAboutContentQuery());
        Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
    }
}
