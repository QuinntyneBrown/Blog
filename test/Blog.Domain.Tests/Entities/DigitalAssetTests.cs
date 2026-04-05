using Xunit;
using Blog.Domain.Entities;
using FluentAssertions;

namespace Blog.Domain.Tests.Entities;

public class DigitalAssetTests
{
    [Fact]
    public void DigitalAsset_DefaultValues_AreCorrect()
    {
        var asset = new DigitalAsset();

        asset.Width.Should().BeNull();
        asset.Height.Should().BeNull();
    }

    [Fact]
    public void DigitalAsset_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var asset = new DigitalAsset
        {
            DigitalAssetId = id,
            OriginalFileName = "hero.jpg",
            StoredFileName = "abc123.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 102400,
            Width = 1920,
            Height = 1080,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        asset.DigitalAssetId.Should().Be(id);
        asset.Width.Should().Be(1920);
        asset.Height.Should().Be(1080);
        asset.FileSizeBytes.Should().Be(102400);
    }
}
