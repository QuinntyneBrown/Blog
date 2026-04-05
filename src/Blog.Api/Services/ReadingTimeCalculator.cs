using System.Text.RegularExpressions;

namespace Blog.Api.Services;

public class ReadingTimeCalculator : IReadingTimeCalculator
{
    private const int WordsPerMinute = 238;

    public int Calculate(string markdownBody)
    {
        var plainText = Regex.Replace(markdownBody, @"[#*_`\[\]()>!|-]", " ");
        var words = plainText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var minutes = (int)Math.Ceiling((double)words.Length / WordsPerMinute);
        return Math.Max(1, minutes);
    }
}
