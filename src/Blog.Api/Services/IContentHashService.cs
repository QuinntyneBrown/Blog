namespace Blog.Api.Services;

/// <summary>
/// Provides content-based hash versioning for static assets, enabling
/// aggressive immutable caching with automatic cache-busting on changes.
/// </summary>
public interface IContentHashService
{
    /// <summary>
    /// Returns a versioned path for the given static asset path.
    /// Example: "/css/app.css" → "/css/app.a1b2c3d4.css"
    /// If the file does not exist, returns the original path unchanged.
    /// </summary>
    string GetHashedPath(string path);

    /// <summary>
    /// Resolves a hashed path back to the original file path.
    /// Example: "/css/app.a1b2c3d4.css" → "/css/app.css"
    /// Returns null if the path does not match the hash pattern.
    /// </summary>
    string? ResolveHashedPath(string hashedPath);
}
