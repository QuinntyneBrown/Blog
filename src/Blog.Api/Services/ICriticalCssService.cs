namespace Blog.Api.Services;

/// <summary>
/// Extracts above-the-fold (critical) CSS from a stylesheet and provides
/// both the inlined critical portion and the deferred full stylesheet path.
/// </summary>
public interface ICriticalCssService
{
    /// <summary>
    /// Returns the critical CSS string for a given stylesheet path.
    /// The critical CSS includes reset/layout rules, navigation, hero, and
    /// above-the-fold article card styles needed for first paint.
    /// </summary>
    string GetCriticalCss(string cssPath);

    /// <summary>
    /// Returns true if the CSS file at the given path exists.
    /// </summary>
    bool CssFileExists(string cssPath);
}
