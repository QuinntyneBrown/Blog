using Blog.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;

namespace Blog.Api.Tests.Services;

public class ContentHashServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ContentHashService _service;
    private readonly MemoryCache _cache;

    public ContentHashServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "blog-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(_tempDir);

        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new ContentHashService(env, _cache);
    }

    [Fact]
    public void GetHashedPath_ExistingFile_ReturnsHashedFilename()
    {
        var cssDir = Path.Combine(_tempDir, "css");
        Directory.CreateDirectory(cssDir);
        File.WriteAllText(Path.Combine(cssDir, "app.css"), "body { color: red; }");

        var result = _service.GetHashedPath("/css/app.css");

        result.Should().MatchRegex(@"/css/app\.[a-f0-9]{8}\.css");
        result.Should().NotBe("/css/app.css");
    }

    [Fact]
    public void GetHashedPath_NonExistentFile_ReturnsOriginalPath()
    {
        var result = _service.GetHashedPath("/css/nonexistent.css");

        result.Should().Be("/css/nonexistent.css");
    }

    [Fact]
    public void GetHashedPath_SameContent_ReturnsSameHash()
    {
        var cssDir = Path.Combine(_tempDir, "css");
        Directory.CreateDirectory(cssDir);
        File.WriteAllText(Path.Combine(cssDir, "a.css"), "same content");
        File.WriteAllText(Path.Combine(cssDir, "b.css"), "same content");

        // Clear cache to force recomputation
        var hash1 = _service.GetHashedPath("/css/a.css");
        var hash2 = _service.GetHashedPath("/css/b.css");

        // Extract just the hash portion
        var hashPart1 = hash1.Split('.')[1];
        var hashPart2 = hash2.Split('.')[1];
        hashPart1.Should().Be(hashPart2);
    }

    [Fact]
    public void GetHashedPath_DifferentContent_ReturnsDifferentHash()
    {
        var cssDir = Path.Combine(_tempDir, "css");
        Directory.CreateDirectory(cssDir);
        File.WriteAllText(Path.Combine(cssDir, "a.css"), "content A");
        File.WriteAllText(Path.Combine(cssDir, "b.css"), "content B");

        var hash1 = _service.GetHashedPath("/css/a.css");
        var hash2 = _service.GetHashedPath("/css/b.css");

        var hashPart1 = hash1.Split('.')[1];
        var hashPart2 = hash2.Split('.')[1];
        hashPart1.Should().NotBe(hashPart2);
    }

    [Fact]
    public void ResolveHashedPath_ValidHashedPath_ReturnsOriginal()
    {
        var result = _service.ResolveHashedPath("/css/app.a1b2c3d4.css");

        result.Should().Be("/css/app.css");
    }

    [Fact]
    public void ResolveHashedPath_NonHashedPath_ReturnsNull()
    {
        var result = _service.ResolveHashedPath("/css/app.css");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveHashedPath_InvalidHashFormat_ReturnsNull()
    {
        // Hash too short
        var result = _service.ResolveHashedPath("/css/app.abc.css");

        result.Should().BeNull();
    }

    [Fact]
    public void GetHashedPath_CachesResult()
    {
        var cssDir = Path.Combine(_tempDir, "css");
        Directory.CreateDirectory(cssDir);
        File.WriteAllText(Path.Combine(cssDir, "app.css"), "body {}");

        var result1 = _service.GetHashedPath("/css/app.css");
        var result2 = _service.GetHashedPath("/css/app.css");

        result1.Should().Be(result2);
    }

    public void Dispose()
    {
        _cache.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
