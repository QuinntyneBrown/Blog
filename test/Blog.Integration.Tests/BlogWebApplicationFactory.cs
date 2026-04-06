using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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

            // Remove hosted services that require a real database or external infrastructure.
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(IHostedService) &&
                     d.ImplementationType != null &&
                     (d.ImplementationType == typeof(MigrationRunner) ||
                      d.ImplementationType == typeof(SeedDataHostedService) ||
                      d.ImplementationType == typeof(Blog.Api.Services.OutboxDispatchService) ||
                      d.ImplementationType == typeof(Blog.Api.Services.NewsletterEmailDispatchService)))
                .ToList();
            foreach (var hs in hostedServices) services.Remove(hs);

            // Add InMemory database.
            services.AddDbContext<BlogDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Provide a valid HMAC key for the unsubscribe token service in tests.
            services.Configure<Blog.Api.Services.UnsubscribeTokenOptions>(opts =>
                opts.HmacKey = "dGVzdC1obWFjLWtleS0zMi1ieXRlcy1sb25nISEh");
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Site:SiteUrl"] = "https://localhost:5001"
            });
        });
    }

    /// <summary>
    /// Seeds the database with a default published article ("Hello World").
    /// Safe to call multiple times; only seeds once per factory instance.
    /// </summary>
    public void EnsureSeeded()
    {
        if (_seeded) return;
        _seeded = true;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var now = DateTime.UtcNow;

        // Seed a test user for auth tests.
        db.Users.Add(new User
        {
            UserId = Guid.NewGuid(),
            Email = "admin@blog.dev",
            PasswordHash = passwordHasher.HashPassword("Admin1234!"),
            DisplayName = "Test Admin",
            CreatedAt = now
        });

        // Seed a published article.
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

        // Seed a draft article.
        db.Articles.Add(new Article
        {
            ArticleId = Guid.NewGuid(),
            Title = "Draft Article",
            Slug = "draft-article",
            Abstract = "This is a draft.",
            Body = "# Draft\n\nNot published yet.",
            BodyHtml = "<h1>Draft</h1><p>Not published yet.</p>",
            Published = false,
            ReadingTimeMinutes = 1,
            Version = 1,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        });

        db.SaveChanges();
    }

    /// <summary>
    /// Creates an authenticated HttpClient by logging in with the seeded test user.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        EnsureSeeded();
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginPayload = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new { email = "admin@blog.dev", password = "Admin1234!" }),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/auth/login", loginPayload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var token = data.GetProperty("token").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
