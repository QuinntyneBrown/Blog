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
    }
}
