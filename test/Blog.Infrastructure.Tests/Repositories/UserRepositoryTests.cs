using Xunit;
using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Blog.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    private static BlogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenGetByEmail_ReturnsUser()
    {
        await using var context = CreateContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            PasswordHash = "hash",
            DisplayName = "Quinn",
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(user);
        await context.SaveChangesAsync();

        var result = await repo.GetByEmailAsync("admin@blog.dev");

        result.Should().NotBeNull();
        result!.Email.Should().Be("admin@blog.dev");
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        await using var context = CreateContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            PasswordHash = "hash",
            DisplayName = "Quinn",
            CreatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(user);
        await context.SaveChangesAsync();

        var result = await repo.GetByEmailAsync("ADMIN@BLOG.DEV");

        result.Should().NotBeNull();
    }
}
