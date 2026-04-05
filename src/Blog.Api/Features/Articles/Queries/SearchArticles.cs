using Blog.Api.Common.Models;
using Blog.Api.Services;
using Blog.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Blog.Api.Features.Articles.Queries;

public record SearchResultDto(
    Guid ArticleId,
    string Title,
    string Slug,
    string Abstract,
    string TitleHighlighted,
    string AbstractHighlighted,
    string? FeaturedImageUrl,
    DateTime? DatePublished,
    int ReadingTimeMinutes);

public record SearchArticlesQuery(string Query, int Page = 1, int PageSize = 10)
    : IRequest<PagedResponse<SearchResultDto>>;

public class SearchArticlesHandler(
    IArticleRepository articles,
    ISearchHighlighter highlighter,
    IConfiguration config) : IRequestHandler<SearchArticlesQuery, PagedResponse<SearchResultDto>>
{
    public async Task<PagedResponse<SearchResultDto>> Handle(
        SearchArticlesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await articles.SearchAsync(
            request.Query, request.Page, request.PageSize, cancellationToken);

        var baseUrl = (config["Site:SiteUrl"] ?? "").TrimEnd('/');

        var dtos = items.Select(a => new SearchResultDto(
            a.ArticleId,
            a.Title,
            a.Slug,
            Truncate(a.Abstract, 160),
            highlighter.Highlight(a.Title, request.Query),
            highlighter.Highlight(Truncate(a.Abstract, 160), request.Query),
            a.FeaturedImage != null ? $"{baseUrl}/assets/{a.FeaturedImage.StoredFileName}" : null,
            a.DatePublished,
            a.ReadingTimeMinutes)).ToList();

        return new PagedResponse<SearchResultDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max].TrimEnd() + "\u2026";
}
