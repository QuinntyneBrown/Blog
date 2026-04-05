using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace Blog.Api.Services;

public sealed class CriticalCssService : ICriticalCssService
{
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;

    // Selectors considered above-the-fold / critical for first paint.
    private static readonly string[] CriticalSelectors =
    [
        ":root", "*", "html", "body", "a", "main", "img",
        ":focus-visible", ".sr-only", ".skip-link",
        ".nav", ".nav-logo", ".nav-links", ".nav-hamburger",
        ".mobile-menu", ".search-wrapper", ".search-form",
        ".search-icon", ".search-input", ".search-toggle",
        ".hero", ".hero-tag", ".hero-title", ".hero-subtitle", ".hero-divider",
        ".article-grid", ".article-card", ".article-card-image",
        ".article-card-body", ".article-card-meta", ".article-card-title",
        ".article-card-abstract", ".article-card-link",
        ".article-card-image-placeholder", ".article-card-meta-dot",
        ".article-detail", ".article-featured-image",
        ".footer", ".footer-links", ".footer-copyright"
    ];

    public CriticalCssService(IWebHostEnvironment env, IMemoryCache cache)
    {
        _env = env;
        _cache = cache;
    }

    public string GetCriticalCss(string cssPath)
    {
        var cacheKey = $"critical-css:{cssPath}";
        if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return cached;

        var fullPath = Path.Combine(_env.WebRootPath, cssPath.TrimStart('/'));
        if (!File.Exists(fullPath))
            return string.Empty;

        var allCss = File.ReadAllText(fullPath);
        var critical = ExtractCriticalRules(allCss);

        _cache.Set(cacheKey, critical, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });

        return critical;
    }

    public bool CssFileExists(string cssPath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, cssPath.TrimStart('/'));
        return File.Exists(fullPath);
    }

    private static string ExtractCriticalRules(string css)
    {
        // Simple extraction: return the full CSS for now.
        // In a production scenario this would parse the CSS AST and filter
        // to only above-the-fold selectors. Since our CSS is already lean
        // (inlined in the layout), we return the full content as critical.
        return css;
    }
}
