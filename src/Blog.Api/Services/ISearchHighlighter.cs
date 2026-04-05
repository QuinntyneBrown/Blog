namespace Blog.Api.Services;

public interface ISearchHighlighter
{
    string Highlight(string text, string query);
}
