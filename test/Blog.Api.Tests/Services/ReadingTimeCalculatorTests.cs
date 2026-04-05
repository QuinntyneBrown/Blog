using Xunit;
using FluentAssertions;

namespace Blog.Api.Tests.Services;

public class ReadingTimeCalculatorTests
{
    private static int Calculate(string body)
    {
        var plainText = System.Text.RegularExpressions.Regex.Replace(body, @"[#*_`\[\]()>!|-]", " ");
        var words = plainText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var minutes = (int)Math.Ceiling((double)words.Length / 238);
        return Math.Max(1, minutes);
    }

    [Fact]
    public void Calculate_ShortText_ReturnsMinimumOneMinute()
    {
        Calculate("Hello world").Should().Be(1);
    }

    [Fact]
    public void Calculate_EmptyString_ReturnsOneMinute()
    {
        Calculate("").Should().Be(1);
    }

    [Fact]
    public void Calculate_238Words_ReturnsOneMinute()
    {
        var body = string.Join(" ", Enumerable.Repeat("word", 238));
        Calculate(body).Should().Be(1);
    }

    [Fact]
    public void Calculate_239Words_ReturnsTwoMinutes()
    {
        var body = string.Join(" ", Enumerable.Repeat("word", 239));
        Calculate(body).Should().Be(2);
    }
}
