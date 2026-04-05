using Xunit;
using Blog.Domain.Entities;
using FluentAssertions;

namespace Blog.Domain.Tests.Entities;

public class ArticleTests
{
    [Fact]
    public void Article_DefaultValues_AreCorrect()
    {
        var article = new Article();

        article.Published.Should().BeFalse();
        article.ReadingTimeMinutes.Should().Be(1);
        article.Version.Should().Be(1);
        article.DatePublished.Should().BeNull();
        article.FeaturedImageId.Should().BeNull();
    }

    [Fact]
    public void Article_CanSetAllProperties()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var article = new Article
        {
            ArticleId = id,
            Title = "Test Title",
            Slug = "test-title",
            Abstract = "Test abstract.",
            Body = "# Hello\nThis is a test.",
            BodyHtml = "<h1>Hello</h1><p>This is a test.</p>",
            Published = true,
            DatePublished = now,
            ReadingTimeMinutes = 3,
            Version = 2,
            CreatedAt = now,
            UpdatedAt = now
        };

        article.ArticleId.Should().Be(id);
        article.Title.Should().Be("Test Title");
        article.Slug.Should().Be("test-title");
        article.Published.Should().BeTrue();
        article.ReadingTimeMinutes.Should().Be(3);
        article.Version.Should().Be(2);
    }
}
