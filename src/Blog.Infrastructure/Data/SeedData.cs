using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Data;

public class SeedData(IUnitOfWork uow, ILogger<SeedData> logger, IConfiguration configuration)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(cancellationToken);
    }

    public async Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        var count = await uow.Articles.GetAllCountAsync(cancellationToken);
        if (count > 0)
        {
            logger.LogDebug("Articles already exist — skipping development seed.");
            return;
        }

        var now = DateTime.UtcNow;
        var sampleArticles = new[]
        {
            new Article
            {
                ArticleId = Guid.NewGuid(),
                Title = "Getting Started with ASP.NET Core",
                Slug = "getting-started-with-aspnet-core",
                Abstract = "A practical introduction to building web applications with ASP.NET Core, covering project setup, middleware, and deployment.",
                Body = "# Getting Started with ASP.NET Core\n\nASP.NET Core is a cross-platform framework for building modern web applications.\n\n## Project Setup\n\nUse `dotnet new webapp` to create a new project.\n\n## Middleware Pipeline\n\nThe middleware pipeline processes every request through a series of components.\n\n## Deployment\n\nPublish with `dotnet publish` and deploy to your hosting platform of choice.",
                BodyHtml = "<h1>Getting Started with ASP.NET Core</h1><p>ASP.NET Core is a cross-platform framework for building modern web applications.</p><h2>Project Setup</h2><p>Use <code>dotnet new webapp</code> to create a new project.</p><h2>Middleware Pipeline</h2><p>The middleware pipeline processes every request through a series of components.</p><h2>Deployment</h2><p>Publish with <code>dotnet publish</code> and deploy to your hosting platform of choice.</p>",
                Published = true, DatePublished = now.AddDays(-7), ReadingTimeMinutes = 3,
                Version = 1, CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-7)
            },
            new Article
            {
                ArticleId = Guid.NewGuid(),
                Title = "Clean Architecture in .NET",
                Slug = "clean-architecture-in-dotnet",
                Abstract = "Exploring how to structure .NET applications using Clean Architecture principles for maintainability and testability.",
                Body = "# Clean Architecture in .NET\n\nClean Architecture separates concerns into layers with clear dependency rules.\n\n## Domain Layer\n\nThe innermost layer contains entities and business rules.\n\n## Application Layer\n\nUse cases and application logic live here.\n\n## Infrastructure Layer\n\nDatabase access, external services, and framework concerns.",
                BodyHtml = "<h1>Clean Architecture in .NET</h1><p>Clean Architecture separates concerns into layers with clear dependency rules.</p><h2>Domain Layer</h2><p>The innermost layer contains entities and business rules.</p><h2>Application Layer</h2><p>Use cases and application logic live here.</p><h2>Infrastructure Layer</h2><p>Database access, external services, and framework concerns.</p>",
                Published = true, DatePublished = now.AddDays(-3), ReadingTimeMinutes = 2,
                Version = 1, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-3)
            }
        };

        foreach (var article in sampleArticles)
            await uow.Articles.AddAsync(article, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} sample articles for development.", sampleArticles.Length);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var seedSection = configuration.GetSection("Seed:AdminUser");
        var email = seedSection["Email"];
        var displayName = seedSection["DisplayName"] ?? "Quinn Brown";
        var passwordHash = seedSection["PasswordHash"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(passwordHash))
        {
            logger.LogDebug("No admin seed user configured — skipping admin user seed.");
            return;
        }

        var existing = await uow.Users.GetByEmailAsync(email, cancellationToken);
        if (existing != null)
        {
            logger.LogDebug("Admin user {Email} already exists — skipping seed.", email);
            return;
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };

        await uow.Users.AddAsync(user, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin user {Email} seeded successfully.", email);
    }
}
