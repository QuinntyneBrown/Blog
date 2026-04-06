using Microsoft.Extensions.Caching.Memory;

namespace Blog.Api.Services;

/// <summary>
/// Evicts ASP.NET Core response cache entries from the underlying <see cref="IMemoryCache"/>
/// when article content changes.
/// </summary>
/// <remarks>
/// ASP.NET Core's <c>ResponseCachingMiddleware</c> stores cached responses in the registered
/// <see cref="IMemoryCache"/> using the normalized request path (e.g. <c>/articles/my-slug</c>)
/// as the cache key.  Removing that key forces the next request to re-render and re-cache,
/// satisfying the design requirement in Section 7.2 of the web-performance design document.
/// </remarks>
public sealed class CacheInvalidator(IMemoryCache cache) : ICacheInvalidator
{
    /// <inheritdoc/>
    public void InvalidateEvent(string slug)
    {
        cache.Remove($"/events/{slug}");
        cache.Remove("/events");
        for (var page = 1; page <= 5; page++)
            cache.Remove($"/events?page={page}");
    }

    /// <inheritdoc/>
    public void InvalidateArticle(string slug)
    {
        // Evict the article detail page.
        cache.Remove($"/articles/{slug}");

        // Evict the paginated listing pages (pages 1–5 cover virtually all realistic blog sizes).
        cache.Remove("/articles");
        for (var page = 1; page <= 5; page++)
            cache.Remove($"/articles?page={page}");

        // Evict the home page (shows latest articles).
        cache.Remove("/");
        for (var page = 1; page <= 5; page++)
            cache.Remove($"/?page={page}");

        // Evict SEO endpoints that include article data.
        // Design reference: docs/detailed-designs/05-seo-and-discoverability/README.md, Section 6.3:
        // "Cache is invalidated when articles are published, unpublished, or modified."
        cache.Remove("/sitemap.xml");
        cache.Remove("/feed.xml");
        cache.Remove("/atom.xml");
        cache.Remove("/feed/json");
    }

    /// <inheritdoc/>
    public void InvalidateAbout()
    {
        cache.Remove("/about");
    }
}
