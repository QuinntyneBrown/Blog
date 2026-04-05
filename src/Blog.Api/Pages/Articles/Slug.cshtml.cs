using Blog.Api.Common.Exceptions;
using Blog.Api.Features.Articles.Queries;
using Blog.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Api.Pages.Articles;

[ResponseCache(CacheProfileName = "HtmlPage")]
public class ArticleDetailModel(IMediator mediator, IETagGenerator eTagGenerator) : PageModel
{
    public ArticleDto? Article { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        try
        {
            var article = await mediator.Send(new GetArticleBySlugQuery(slug));
            if (!article.Published)
            {
                Article = null;
                return Page();
            }

            // Compute the weak ETag from stable article metadata (ID + Version).
            // Design reference: docs/detailed-designs/07-web-performance/README.md, Section 3.7.
            var etag = eTagGenerator.Generate(article.ArticleId, article.Version);

            // If the client already holds the current version, short-circuit with 304.
            var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
            if (eTagGenerator.IsMatch(etag, ifNoneMatch))
                return StatusCode(304);

            Article = article;
            Response.Headers.ETag = etag;
            Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600");
            return Page();
        }
        catch (NotFoundException)
        {
            Article = null;
            return Page();
        }
    }
}
