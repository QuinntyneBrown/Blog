using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace Blog.Api.Services;

public sealed partial class ContentHashService : IContentHashService
{
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;

    [GeneratedRegex(@"^(.+)\.([a-f0-9]{8})(\.[^.]+)$")]
    private static partial Regex HashedPathPattern();

    public ContentHashService(IWebHostEnvironment env, IMemoryCache cache)
    {
        _env = env;
        _cache = cache;
    }

    public string GetHashedPath(string path)
    {
        var cacheKey = $"content-hash:{path}";
        if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return cached;

        var fullPath = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
        if (!File.Exists(fullPath))
            return path;

        var hash = ComputeFileHash(fullPath);
        var ext = Path.GetExtension(path);
        var basePath = path[..^ext.Length];
        var hashedPath = $"{basePath}.{hash}{ext}";

        _cache.Set(cacheKey, hashedPath, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });

        return hashedPath;
    }

    public string? ResolveHashedPath(string hashedPath)
    {
        var match = HashedPathPattern().Match(hashedPath.TrimStart('/'));
        if (!match.Success)
            return null;

        var basePath = match.Groups[1].Value;
        var ext = match.Groups[3].Value;
        return $"/{basePath}{ext}";
    }

    private static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes)[..8].ToLowerInvariant();
    }
}
