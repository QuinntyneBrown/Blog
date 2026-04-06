namespace Blog.Api.Services;

/// <summary>
/// Generates weak ETag validators for article detail HTML pages and compares them
/// against incoming <c>If-None-Match</c> request headers to enable 304 responses.
/// </summary>
/// <remarks>
/// Design reference: docs/detailed-designs/07-web-performance/README.md, Section 3.7.
/// Weak ETags (<c>W/"..."</c>) are used because compressed and uncompressed
/// representations share the same validator, consistent with the design's specification.
/// </remarks>
public class ETagGenerator : IETagGenerator
{
    /// <inheritdoc />
    public string Generate(Guid articleId, int version)
        => $"W/\"article-{articleId}-v{version}\"";

    /// <inheritdoc />
    public string GenerateAbout(int version)
        => $"W/\"about:{version}\"";

    /// <inheritdoc />
    public bool IsMatch(string etag, string? ifNoneMatch)
    {
        if (string.IsNullOrEmpty(ifNoneMatch))
            return false;

        // The If-None-Match header may contain a comma-separated list of ETags or "*".
        if (ifNoneMatch.Trim() == "*")
            return true;

        foreach (var candidate in ifNoneMatch.Split(','))
        {
            if (candidate.Trim().Equals(etag, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
