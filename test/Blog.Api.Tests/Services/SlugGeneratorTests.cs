using Xunit;
using FluentAssertions;

namespace Blog.Api.Tests.Services;

public class SlugGeneratorTests
{
    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("My First Blog Post!", "my-first-blog-post")]
    [InlineData("C# and .NET", "c-and-net")]
    [InlineData("  Leading and trailing  ", "leading-and-trailing")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    public void Generate_ProducesCorrectSlug(string title, string expected)
    {
        GenerateSlug(title).Should().Be(expected);
    }
}
