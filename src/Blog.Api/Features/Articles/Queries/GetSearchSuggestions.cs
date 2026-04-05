using Blog.Api.Services;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Articles.Queries;

public record SearchSuggestionDto(
    string Title,
    string Slug,
    string TitleHighlighted);

public record GetSearchSuggestionsQuery(string Query)
    : IRequest<IReadOnlyList<SearchSuggestionDto>>;

public class GetSearchSuggestionsHandler(
    IArticleRepository articles,
    ISearchHighlighter highlighter)
    : IRequestHandler<GetSearchSuggestionsQuery, IReadOnlyList<SearchSuggestionDto>>
{
    public async Task<IReadOnlyList<SearchSuggestionDto>> Handle(
        GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var items = await articles.GetSuggestionsAsync(request.Query, cancellationToken);
        return items.Select(a => new SearchSuggestionDto(
            a.Title,
            a.Slug,
            highlighter.Highlight(a.Title, request.Query))).ToList();
    }
}
