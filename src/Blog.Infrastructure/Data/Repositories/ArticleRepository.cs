using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Blog.Infrastructure.Data.Repositories;

public class ArticleRepository(BlogDbContext context) : IArticleRepository
{
    public async Task<Article?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken = default)
        => await context.Articles.Include(a => a.FeaturedImage).FirstOrDefaultAsync(a => a.ArticleId == articleId, cancellationToken);

    public async Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Articles.Include(a => a.FeaturedImage).FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Article>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        // Body and BodyHtml are nvarchar(max) columns excluded from list projections per design 02, Section 4.2.
        // The Select projection omits those columns so the SQL query does not transfer large content for listing queries.
        // FeaturedImage is included via the navigation property inside Select (EF Core translates it to a LEFT JOIN).
        => await context.Articles
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new Article
            {
                ArticleId = a.ArticleId,
                Title = a.Title,
                Slug = a.Slug,
                Abstract = a.Abstract,
                Body = string.Empty,
                BodyHtml = string.Empty,
                FeaturedImageId = a.FeaturedImageId,
                FeaturedImage = a.FeaturedImage,
                Published = a.Published,
                DatePublished = a.DatePublished,
                ReadingTimeMinutes = a.ReadingTimeMinutes,
                Version = a.Version,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync(cancellationToken);

    public async Task<int> GetAllCountAsync(CancellationToken cancellationToken = default)
        => await context.Articles.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        // Body and BodyHtml are nvarchar(max) columns excluded from list projections per design 02, Section 4.2.
        => await context.Articles
            .AsNoTracking()
            .Where(a => a.Published)
            .OrderByDescending(a => a.DatePublished)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new Article
            {
                ArticleId = a.ArticleId,
                Title = a.Title,
                Slug = a.Slug,
                Abstract = a.Abstract,
                Body = string.Empty,
                BodyHtml = string.Empty,
                FeaturedImageId = a.FeaturedImageId,
                FeaturedImage = a.FeaturedImage,
                Published = a.Published,
                DatePublished = a.DatePublished,
                ReadingTimeMinutes = a.ReadingTimeMinutes,
                Version = a.Version,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            })
            .ToListAsync(cancellationToken);

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        => await context.Articles.CountAsync(a => a.Published, cancellationToken);

    public async Task<(IReadOnlyList<Article> Articles, int TotalCount)> SearchAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var ftsQuery = BuildFtsQuery(query);
        var offset = (page - 1) * pageSize;

        var sql = @"
            SELECT a.ArticleId, a.Title, a.Slug, a.Abstract,
                   a.FeaturedImageId, a.Published, a.DatePublished,
                   a.ReadingTimeMinutes, a.CreatedAt, a.UpdatedAt, a.Version,
                   a.Body, a.BodyHtml, a.CreatedBy
            FROM Articles a
            INNER JOIN CONTAINSTABLE(Articles, (Title, Abstract, Body), {0})
                AS KEY_TBL ON a.ArticleId = KEY_TBL.[KEY]
            WHERE a.Published = 1
            ORDER BY KEY_TBL.RANK DESC, a.DatePublished DESC
            OFFSET {1} ROWS FETCH NEXT {2} ROWS ONLY";

        var countSql = @"
            SELECT COUNT(*)
            FROM Articles a
            INNER JOIN CONTAINSTABLE(Articles, (Title, Abstract, Body), {0})
                AS KEY_TBL ON a.ArticleId = KEY_TBL.[KEY]
            WHERE a.Published = 1";

        var articles = await context.Articles
            .FromSqlRaw(sql, ftsQuery, offset, pageSize)
            .Include(a => a.FeaturedImage)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalResult = await context.Database
            .SqlQueryRaw<int>(countSql, ftsQuery)
            .ToListAsync(cancellationToken);
        var total = totalResult.FirstOrDefault();

        return (articles, total);
    }

    public async Task<IReadOnlyList<Article>> GetSuggestionsAsync(
        string query, CancellationToken cancellationToken = default)
    {
        var ftsQuery = $"\"{query.Trim().Replace("\"", "")}*\"";
        return await context.Articles
            .FromSqlRaw(@"
                SELECT TOP 8 ArticleId, Title, Slug,
                       Abstract, FeaturedImageId, Published,
                       DatePublished, ReadingTimeMinutes,
                       CreatedAt, UpdatedAt, Version, Body, BodyHtml, CreatedBy
                FROM Articles
                WHERE Published = 1
                  AND CONTAINS(Title, {0})
                ORDER BY DatePublished DESC",
                ftsQuery)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private static string BuildFtsQuery(string raw)
    {
        var terms = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => $"\"{t.Replace("\"", "")}*\"");
        return string.Join(" AND ", terms);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await context.Articles.AnyAsync(a => a.Slug == slug && (excludeId == null || a.ArticleId != excludeId), cancellationToken);

    public async Task<bool> AnyByFeaturedImageIdAsync(Guid digitalAssetId, CancellationToken cancellationToken = default)
        => await context.Articles.AnyAsync(a => a.FeaturedImageId == digitalAssetId, cancellationToken);

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
        => await context.Articles.AddAsync(article, cancellationToken);

    public void Update(Article article) => context.Articles.Update(article);
    public void Remove(Article article) => context.Articles.Remove(article);
}
