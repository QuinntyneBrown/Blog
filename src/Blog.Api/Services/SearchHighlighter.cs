using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Blog.Api.Services;

public class SearchHighlighter : ISearchHighlighter
{
    public string Highlight(string text, string query)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            return HtmlEncoder.Default.Encode(text ?? string.Empty);

        // HTML-encode the source text first so no existing content can break HTML context.
        var encoded = HtmlEncoder.Default.Encode(text);

        foreach (var term in query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var escapedTerm = Regex.Escape(HtmlEncoder.Default.Encode(term));
            encoded = Regex.Replace(
                encoded,
                $@"(?i){escapedTerm}",
                m => $"<mark>{m.Value}</mark>");
        }

        return encoded;
    }
}
