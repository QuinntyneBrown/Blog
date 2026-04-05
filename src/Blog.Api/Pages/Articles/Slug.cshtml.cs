using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Articles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Articles;

[ResponseCache(CacheProfileName = "HtmlPage")]
public class ArticleDetailModel(IMediator mediator) : PageModel
{
    public ArticleDto? Article { get; private set; }

    public async Task OnGetAsync(string slug)
    {
        try
        {
            var article = await mediator.Send(new GetArticleBySlugQuery(slug));
            if (!article.Published)
            {
                Article = null;
                return;
            }
            Article = article;
            Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
        }
        catch (NotFoundException)
        {
            Article = null;
        }
    }
}
