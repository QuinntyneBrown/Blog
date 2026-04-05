using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Integration.Tests;

public class BlogWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "BlogIntegrationTests_" + Guid.NewGuid().ToString("N");
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations (pool + options).
            var dbDescriptors = services.Where(
                d => d.ServiceType.FullName?.Contains("BlogDbContext") == true ||
                     d.ServiceType == typeof(DbContextOptions<BlogDbContext>))
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);

            // Remove hosted services that require a real database.
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                     d.ImplementationType != null &&
                     (d.ImplementationType == typeof(MigrationRunner) ||
                      d.ImplementationType == typeof(SeedDataHostedService)))
                .ToList();
            foreach (var hs in hostedServices) services.Remove(hs);

            // Add InMemory database.
            services.AddDbContext<BlogDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public void EnsureSeeded()
    {
        if (_seeded) return;
        _seeded = true;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var now = DateTime.UtcNow;
        db.Articles.Add(new Article
        {
            ArticleId = Guid.NewGuid(),
            Title = "Hello World",
            Slug = "hello-world",
            Abstract = "Welcome to the blog!",
            Body = "# Hello World\n\nContent here.",
            BodyHtml = "<h1>Hello World</h1><p>Content here.</p>",
            Published = true,
            DatePublished = now.AddDays(-5),
            ReadingTimeMinutes = 2,
            Version = 1,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-5)
        });
        db.SaveChanges();
    }
}
