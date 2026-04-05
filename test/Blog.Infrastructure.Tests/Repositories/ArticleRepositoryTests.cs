using Xunit;
using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Blog.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Tests.Repositories;

public class ArticleRepositoryTests
{
    private static BlogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsArticle()
    {
        await using var context = CreateContext();
        var repo = new ArticleRepository(context);
        var article = new Article
        {
            ArticleId = Guid.NewGuid(),
            Title = "Test",
            Slug = "test",
            Abstract = "abstract",
            Body = "body",
            BodyHtml = "<p>body</p>",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(article);
        await context.SaveChangesAsync();

        var result = await repo.GetByIdAsync(article.ArticleId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsCorrectArticle()
    {
        await using var context = CreateContext();
        var repo = new ArticleRepository(context);
        var article = new Article
        {
            ArticleId = Guid.NewGuid(),
            Title = "Hello",
            Slug = "hello",
            Abstract = "abstract",
            Body = "body",
            BodyHtml = "<p>body</p>",
            Published = true,
            DatePublished = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(article);
        await context.SaveChangesAsync();

        var result = await repo.GetBySlugAsync("hello");

        result.Should().NotBeNull();
        result!.Slug.Should().Be("hello");
    }

    [Fact]
    public async Task SlugExistsAsync_ReturnsTrueForExistingSlug()
    {
        await using var context = CreateContext();
        var repo = new ArticleRepository(context);
        var article = new Article
        {
            ArticleId = Guid.NewGuid(), Title = "X", Slug = "x",
            Abstract = "a", Body = "b", BodyHtml = "<p>b</p>",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(article);
        await context.SaveChangesAsync();

        var exists = await repo.SlugExistsAsync("x");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Remove_DeletesArticle()
    {
        await using var context = CreateContext();
        var repo = new ArticleRepository(context);
        var article = new Article
        {
            ArticleId = Guid.NewGuid(), Title = "Delete Me", Slug = "delete-me",
            Abstract = "a", Body = "b", BodyHtml = "<p>b</p>",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(article);
        await context.SaveChangesAsync();

        repo.Remove(article);
        await context.SaveChangesAsync();

        var result = await repo.GetByIdAsync(article.ArticleId);
        result.Should().BeNull();
    }
}
