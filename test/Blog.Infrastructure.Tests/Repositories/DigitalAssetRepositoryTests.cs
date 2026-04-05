using Xunit;
using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Blog.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Tests.Repositories;

public class DigitalAssetRepositoryTests
{
    private static BlogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenGetAll_ReturnsAsset()
    {
        await using var context = CreateContext();
        // Need a user first (FK constraint not enforced in InMemory)
        var userId = Guid.NewGuid();
        var repo = new DigitalAssetRepository(context);
        var asset = new DigitalAsset
        {
            DigitalAssetId = Guid.NewGuid(),
            OriginalFileName = "img.jpg",
            StoredFileName = "abc.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await repo.AddAsync(asset);
        await context.SaveChangesAsync();

        var result = await repo.GetAllAsync();
        result.Should().ContainSingle(a => a.OriginalFileName == "img.jpg");
    }
}
