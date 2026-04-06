namespace Blog.Api.Services;

/// <summary>
/// Computes and validates weak ETag validators for cacheable HTML pages.
/// </summary>
/// <remarks>
/// Design reference: docs/detailed-designs/07-web-performance/README.md, Section 3.7.
/// The ETag is derived from stable page metadata (e.g., entity ID + Version) rather
/// than from a hash of the rendered HTML, so the value is known before rendering begins.
/// </remarks>
public interface IETagGenerator
{
    /// <summary>
    /// Builds the weak ETag string for an article page in the form
    /// <c>W/"article-{articleId}-v{version}"</c>.
    /// </summary>
    string Generate(Guid articleId, int version);

    /// <summary>
    /// Builds the weak ETag string for the about page in the form
    /// <c>W/"about:{version}"</c>.
    /// </summary>
    string GenerateAbout(int version);

    /// <summary>
    /// Returns <see langword="true"/> when the client-supplied <paramref name="ifNoneMatch"/>
    /// header value matches the <paramref name="etag"/>, indicating the client already holds
    /// the current representation and a 304 Not Modified response can be issued.
    /// </summary>
    bool IsMatch(string etag, string? ifNoneMatch);
}
