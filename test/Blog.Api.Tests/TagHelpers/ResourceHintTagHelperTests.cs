using Blog.Api.TagHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Blog.Api.Tests.TagHelpers;

public class ResourceHintTagHelperTests
{
    [Fact]
    public void Process_Preconnect_GeneratesCorrectLink()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "preconnect",
            Href = "https://fonts.googleapis.com"
        };

        var output = Execute(tagHelper);

        output.Should().Contain("rel=\"preconnect\"");
        output.Should().Contain("href=\"https://fonts.googleapis.com\"");
        output.Should().NotContain("crossorigin");
    }

    [Fact]
    public void Process_PreconnectWithCrossorigin_IncludesCrossoriginAttribute()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "preconnect",
            Href = "https://fonts.gstatic.com",
            Crossorigin = true
        };

        var output = Execute(tagHelper);

        output.Should().Contain("rel=\"preconnect\"");
        output.Should().Contain("crossorigin");
    }

    [Fact]
    public void Process_DnsPrefetch_GeneratesCorrectLink()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "dns-prefetch",
            Href = "https://fonts.googleapis.com"
        };

        var output = Execute(tagHelper);

        output.Should().Contain("rel=\"dns-prefetch\"");
    }

    [Fact]
    public void Process_Preload_IncludesAsAttribute()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "preload",
            Href = "/css/app.css",
            As = "style"
        };

        var output = Execute(tagHelper);

        output.Should().Contain("rel=\"preload\"");
        output.Should().Contain("as=\"style\"");
    }

    [Fact]
    public void Process_PreloadFont_IncludesCrossoriginAndAs()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "preload",
            Href = "/fonts/inter.woff2",
            As = "font",
            Crossorigin = true
        };

        var output = Execute(tagHelper);

        output.Should().Contain("rel=\"preload\"");
        output.Should().Contain("as=\"font\"");
        output.Should().Contain("crossorigin");
    }

    [Fact]
    public void Process_EmptyHref_ProducesNoOutput()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "preconnect",
            Href = ""
        };

        var output = Execute(tagHelper);

        output.Should().BeEmpty();
    }

    [Fact]
    public void Process_WithNonce_IncludesNonceAttribute()
    {
        var tagHelper = new ResourceHintTagHelper
        {
            HintType = "preload",
            Href = "/css/app.css",
            As = "style",
            Nonce = "abc123"
        };

        var output = Execute(tagHelper);

        output.Should().Contain("nonce=\"abc123\"");
    }

    private static string Execute(ResourceHintTagHelper tagHelper)
    {
        var context = new TagHelperContext(
            tagName: "resource-hint",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString());

        var output = new TagHelperOutput(
            "resource-hint",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        tagHelper.Process(context, output);

        return output.Content.GetContent();
    }
}
