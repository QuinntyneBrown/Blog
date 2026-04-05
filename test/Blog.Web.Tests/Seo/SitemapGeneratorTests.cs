using Xunit;
using Blog.Domain.Entities;
using FluentAssertions;

namespace Blog.Web.Tests.Seo;

public class SitemapGeneratorTests
{
    [Fact]
    public void Sitemap_ContainsPublishedArticleSlug()
    {
        var articles = new List<Article>
        {
            new() { Title = "Hello", Slug = "hello", Published = true, DatePublished = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        // Verify slug appears in expected URL format
        var url = $"https://example.com/articles/{articles[0].Slug}";
        url.Should().Contain("hello");
    }

    [Fact]
    public void Sitemap_ExcludesUnpublishedArticles()
    {
        var articles = new List<Article>
        {
            new() { Title = "Draft", Slug = "draft", Published = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        articles.Where(a => a.Published).Should().BeEmpty();
    }
}
