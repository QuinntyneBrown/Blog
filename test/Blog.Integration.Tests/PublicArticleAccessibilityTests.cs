using System.Net;
using FluentAssertions;
using Blog.Domain.Entities;
using Blog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Blog.Integration.Tests;

public class PublicArticleAccessibilityTests : IClassFixture<BlogWebApplicationFactory>
{
    private readonly BlogWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublicArticleAccessibilityTests(BlogWebApplicationFactory factory)
    {
        _factory = factory;
        factory.EnsureSeeded();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetArticles_ContainsMainLandmarkAndSkipLink()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<main role=\"main\" id=\"main-content\">");
        html.Should().Contain("href=\"#main-content\"");
    }

    [Fact]
    public async Task GetArticles_ContainsFooterLandmarkOutsideMain()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<footer class=\"footer\" role=\"contentinfo\">");

        // Footer must come after </main>, not nested inside it
        var mainCloseIndex = html.IndexOf("</main>");
        var footerIndex = html.IndexOf("<footer");
        mainCloseIndex.Should().BeGreaterThan(-1, "page should contain </main>");
        footerIndex.Should().BeGreaterThan(-1, "page should contain <footer>");
        footerIndex.Should().BeGreaterThan(mainCloseIndex, "<footer> must appear after </main>");
    }

    [Fact]
    public async Task GetArticles_ContainsFooterNavigation()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("aria-label=\"Footer navigation\"");
    }

    [Fact]
    public async Task GetArticleBySlug_MissingArticle_Returns404()
    {
        var response = await _client.GetAsync("/articles/non-existent-article");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticleBySlug_MixedCaseSlug_Returns301Redirect()
    {
        var response = await _client.GetAsync("/articles/My-Article");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be("/articles/my-article");
    }

    [Fact]
    public async Task GetArticleBySlug_TrailingSlash_Returns301Redirect()
    {
        var response = await _client.GetAsync("/articles/my-article/");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be("/articles/my-article");
    }
}

public class PaginationAccessibilityTests : IClassFixture<PaginationTestFactory>
{
    private readonly HttpClient _client;

    public PaginationAccessibilityTests(PaginationTestFactory factory)
    {
        factory.EnsurePaginationSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetArticles_WithPagination_ContainsPaginationNav()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<nav class=\"pagination\" aria-label=\"Pagination\">");
    }

    [Fact]
    public async Task GetArticles_WithPagination_ContainsAriaCurrentOnActivePage()
    {
        var response = await _client.GetAsync("/articles");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("aria-current=\"page\"");
    }
}

/// <summary>
/// A factory that seeds enough articles to trigger pagination (page size is 9).
/// </summary>
public class PaginationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "PaginationTestDb_" + Guid.NewGuid().ToString("N");
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbDescriptors = services.Where(
                d => d.ServiceType.FullName?.Contains("BlogDbContext") == true ||
                     d.ServiceType == typeof(DbContextOptions<BlogDbContext>))
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);

            var hostedServices = services.Where(
                d => d.ServiceType == typeof(IHostedService) &&
                     d.ImplementationType != null &&
                     (d.ImplementationType == typeof(MigrationRunner) ||
                      d.ImplementationType == typeof(SeedDataHostedService)))
                .ToList();
            foreach (var hs in hostedServices) services.Remove(hs);

            services.AddDbContext<BlogDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public void EnsurePaginationSeeded()
    {
        if (_seeded) return;
        _seeded = true;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var now = DateTime.UtcNow;

        for (int i = 1; i <= 12; i++)
        {
            db.Articles.Add(new Article
            {
                ArticleId = Guid.NewGuid(),
                Title = $"Test Article {i}",
                Slug = $"test-article-{i}",
                Abstract = $"Abstract for article {i}",
                Body = $"Body of article {i}",
                BodyHtml = $"<p>Body of article {i}</p>",
                Published = true,
                DatePublished = now.AddDays(-i),
                ReadingTimeMinutes = 2,
                Version = 1,
                CreatedAt = now.AddDays(-i),
                UpdatedAt = now.AddDays(-i)
            });
        }
        db.SaveChanges();
    }
}
