namespace Blog.Api.Common.Models;

/// <summary>
/// Generates pagination navigation URLs for <see cref="PagedResponse{T}"/> objects.
/// URLs are built from the configured base URL and current request path to avoid
/// host-header injection (consistent with Feature 06, Section 3.3).
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Populates <see cref="PagedResponse{T}.PreviousPageUrl"/> and
    /// <see cref="PagedResponse{T}.NextPageUrl"/> using the supplied base URL
    /// and request path.
    /// </summary>
    /// <param name="response">The paged response to enrich.</param>
    /// <param name="baseUrl">Configured site base URL (e.g. "https://example.com"). Trailing slashes are trimmed.</param>
    /// <param name="requestPath">The current request path (e.g. "/api/articles").</param>
    public static void SetNavigationUrls<T>(PagedResponse<T> response, string baseUrl, string requestPath)
    {
        if (response.PageSize <= 0)
            return;

        var trimmedBase = baseUrl.TrimEnd('/');

        if (response.HasPreviousPage)
            response.PreviousPageUrl = $"{trimmedBase}{requestPath}?page={response.Page - 1}&pageSize={response.PageSize}";

        if (response.HasNextPage)
            response.NextPageUrl = $"{trimmedBase}{requestPath}?page={response.Page + 1}&pageSize={response.PageSize}";
    }
}
