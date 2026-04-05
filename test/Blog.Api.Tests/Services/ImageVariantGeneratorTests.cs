using Blog.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Blog.Api.Tests.Services;

public class ImageVariantGeneratorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ILogger<ImageVariantGenerator> _logger;
    private readonly ImageVariantGenerator _generator;

    public ImageVariantGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"variant-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _logger = Substitute.For<ILogger<ImageVariantGenerator>>();
        _generator = new ImageVariantGenerator(_logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTestImage(int width, int height)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.png");
        using var image = new Image<Rgba32>(width, height);
        image.SaveAsPng(path);
        return path;
    }

    [Fact]
    public async Task GenerateVariantsAsync_CreatesWebpVariantsForBreakpointsBelowOriginalWidth()
    {
        // Arrange: 2000px-wide image → variants at 320, 640, 960, 1280, 1920
        var assetId = Guid.NewGuid();
        var sourcePath = CreateTestImage(2000, 1000);

        // Act
        await _generator.GenerateVariantsAsync(sourcePath, assetId, 2000);

        // Assert
        int[] expectedBreakpoints = [320, 640, 960, 1280, 1920];
        foreach (var bp in expectedBreakpoints)
        {
            var webpPath = Path.Combine(_tempDir, $"{assetId}-{bp}w.webp");
            File.Exists(webpPath).Should().BeTrue($"WebP variant at {bp}w should exist");
        }
    }

    [Fact]
    public async Task GenerateVariantsAsync_CreatesAvifVariantsForBreakpointsBelowOriginalWidth()
    {
        // Arrange: 2000px-wide image → AVIF variants at 320, 640, 960, 1280, 1920
        var assetId = Guid.NewGuid();
        var sourcePath = CreateTestImage(2000, 1000);

        // Act
        await _generator.GenerateVariantsAsync(sourcePath, assetId, 2000);

        // Assert
        int[] expectedBreakpoints = [320, 640, 960, 1280, 1920];
        foreach (var bp in expectedBreakpoints)
        {
            var avifPath = Path.Combine(_tempDir, $"{assetId}-{bp}w.avif");
            File.Exists(avifPath).Should().BeTrue($"AVIF variant at {bp}w should exist");
        }
    }

    [Fact]
    public async Task GenerateVariantsAsync_SkipsBreakpointsWiderThanOrEqualToOriginal()
    {
        // Arrange: 640px-wide image → only 320 breakpoint (below 640)
        var assetId = Guid.NewGuid();
        var sourcePath = CreateTestImage(640, 480);

        // Act
        await _generator.GenerateVariantsAsync(sourcePath, assetId, 640);

        // Assert: only 320w variants should exist
        File.Exists(Path.Combine(_tempDir, $"{assetId}-320w.webp")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, $"{assetId}-320w.avif")).Should().BeTrue();

        // Breakpoints >= 640 should NOT exist
        File.Exists(Path.Combine(_tempDir, $"{assetId}-640w.webp")).Should().BeFalse();
        File.Exists(Path.Combine(_tempDir, $"{assetId}-640w.avif")).Should().BeFalse();
        File.Exists(Path.Combine(_tempDir, $"{assetId}-960w.webp")).Should().BeFalse();
        File.Exists(Path.Combine(_tempDir, $"{assetId}-960w.avif")).Should().BeFalse();
    }

    [Fact]
    public async Task GenerateVariantsAsync_ImageNarrowerThan320_GeneratesZeroVariants()
    {
        // Arrange: 200px-wide image → no breakpoints below 200 (320 >= 200 would be upscaling)
        // Wait, 320 >= 200 means skip. Actually, the condition is breakpointWidth >= originalWidth,
        // so for originalWidth=200, all breakpoints (320, 640, ...) are >= 200, so ALL are skipped.
        // But actually, 320 >= 200 is true, so it's skipped. Correct: zero variants.
        var assetId = Guid.NewGuid();
        var sourcePath = CreateTestImage(200, 150);

        // Act
        await _generator.GenerateVariantsAsync(sourcePath, assetId, 200);

        // Assert: no variant files should be created
        var variantFiles = Directory.GetFiles(_tempDir, $"{assetId}-*");
        variantFiles.Should().BeEmpty("no variants should be generated for an image narrower than 320px");
    }

    [Fact]
    public async Task GenerateVariantsAsync_AvifVariantFilesHaveCorrectNamingConvention()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var sourcePath = CreateTestImage(1000, 500);

        // Act
        await _generator.GenerateVariantsAsync(sourcePath, assetId, 1000);

        // Assert: naming convention is {assetId}-{width}w.avif
        int[] expectedBreakpoints = [320, 640, 960];
        foreach (var bp in expectedBreakpoints)
        {
            var expectedFileName = $"{assetId}-{bp}w.avif";
            File.Exists(Path.Combine(_tempDir, expectedFileName)).Should().BeTrue(
                $"AVIF variant should follow naming convention: {expectedFileName}");
        }
    }

    [Fact]
    public async Task GenerateVariantsAsync_BothFormatsGeneratedPerBreakpoint()
    {
        // Arrange: 1000px image → breakpoints 320, 640, 960 (each should have both .webp and .avif)
        var assetId = Guid.NewGuid();
        var sourcePath = CreateTestImage(1000, 500);

        // Act
        await _generator.GenerateVariantsAsync(sourcePath, assetId, 1000);

        // Assert: each breakpoint should have both WebP and AVIF variants
        int[] expectedBreakpoints = [320, 640, 960];
        foreach (var bp in expectedBreakpoints)
        {
            File.Exists(Path.Combine(_tempDir, $"{assetId}-{bp}w.webp")).Should().BeTrue(
                $"WebP variant at {bp}w should exist");
            File.Exists(Path.Combine(_tempDir, $"{assetId}-{bp}w.avif")).Should().BeTrue(
                $"AVIF variant at {bp}w should exist");
        }

        // 1280 and 1920 should NOT exist (>= 1000)
        File.Exists(Path.Combine(_tempDir, $"{assetId}-1280w.webp")).Should().BeFalse();
        File.Exists(Path.Combine(_tempDir, $"{assetId}-1280w.avif")).Should().BeFalse();
    }
}
