namespace Blog.Api.Services;

/// <summary>
/// Evicts in-memory response cache entries for article-related pages.
/// Called after publish and update operations so the next request triggers a fresh render
/// instead of serving stale cached HTML.
/// </summary>
/// <remarks>
/// Design reference: docs/detailed-designs/07-web-performance/README.md, Section 3.1 and Section 7.2.
/// </remarks>
public interface ICacheInvalidator
{
    /// <summary>
    /// Evicts the response cache entry for the article detail page at the given slug
    /// and for the home and article listing pages (which show article metadata).
    /// </summary>
    void InvalidateArticle(string slug);
}
