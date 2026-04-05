using Xunit;
using Blog.Domain.Entities;
using FluentAssertions;

namespace Blog.Domain.Tests.Entities;

public class AboutContentTests
{
    [Fact]
    public void AboutContent_DefaultValues_AreCorrect()
    {
        var aboutContent = new AboutContent();

        aboutContent.Version.Should().Be(1);
        aboutContent.ProfileImageId.Should().BeNull();
        aboutContent.Heading.Should().BeEmpty();
        aboutContent.Body.Should().BeEmpty();
        aboutContent.BodyHtml.Should().BeEmpty();
    }

    [Fact]
    public void AboutContent_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var aboutContent = new AboutContent
        {
            AboutContentId = id,
            Heading = "About Me",
            Body = "# Hello\nI am a developer.",
            BodyHtml = "<h1>Hello</h1><p>I am a developer.</p>",
            ProfileImageId = imageId,
            Version = 2,
            CreatedAt = now,
            UpdatedAt = now
        };

        aboutContent.AboutContentId.Should().Be(id);
        aboutContent.Heading.Should().Be("About Me");
        aboutContent.Body.Should().Be("# Hello\nI am a developer.");
        aboutContent.BodyHtml.Should().Be("<h1>Hello</h1><p>I am a developer.</p>");
        aboutContent.ProfileImageId.Should().Be(imageId);
        aboutContent.Version.Should().Be(2);
        aboutContent.CreatedAt.Should().Be(now);
        aboutContent.UpdatedAt.Should().Be(now);
    }
}
