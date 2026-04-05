using Blog.Api.TagHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Blog.Api.Tests.TagHelpers;

public class ResponsiveImageTagHelperTests
{
    [Fact]
    public void Process_WithResponsiveAttribute_GeneratesPictureElement()
    {
        var tagHelper = new ResponsiveImageTagHelper
        {
            Src = "/assets/abc123.jpg",
            AssetId = "abc123",
            Alt = "Test image",
            ImgWidth = 1280,
            ImgHeight = 720,
            Sizes = "(max-width: 768px) 100vw, 1280px",
            Priority = true,
            Responsive = true
        };

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("img");

        tagHelper.Process(context, output);

        var content = output.Content.GetContent();
        content.Should().Contain("<picture>");
        content.Should().Contain("</picture>");
        content.Should().Contain("image/avif");
        content.Should().Contain("image/webp");
        content.Should().Contain("abc123-320w.avif");
        content.Should().Contain("abc123-1920w.avif");
        content.Should().Contain("abc123-320w.webp");
        content.Should().Contain("abc123-1920w.webp");
        content.Should().Contain("width=\"1280\"");
        content.Should().Contain("height=\"720\"");
        content.Should().Contain("loading=\"eager\"");
        content.Should().Contain("fetchpriority=\"high\"");
    }

    [Fact]
    public void Process_WithLazyLoading_UsesLazyAttributes()
    {
        var tagHelper = new ResponsiveImageTagHelper
        {
            Src = "/assets/abc123.jpg",
            AssetId = "abc123",
            Alt = "Test image",
            Priority = false,
            Responsive = true
        };

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("img");

        tagHelper.Process(context, output);

        var content = output.Content.GetContent();
        content.Should().Contain("loading=\"lazy\"");
        content.Should().Contain("decoding=\"async\"");
        content.Should().NotContain("fetchpriority");
        // Card breakpoints: 320, 640, 960
        content.Should().Contain("abc123-320w.avif");
        content.Should().Contain("abc123-640w.avif");
        content.Should().Contain("abc123-960w.avif");
        content.Should().NotContain("abc123-1280w.avif");
    }

    [Fact]
    public void Process_WithCustomBreakpoints_UsesSpecifiedBreakpoints()
    {
        var tagHelper = new ResponsiveImageTagHelper
        {
            Src = "/assets/abc123.jpg",
            AssetId = "abc123",
            Alt = "Test image",
            Breakpoints = "400,800",
            Priority = false,
            Responsive = true
        };

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("img");

        tagHelper.Process(context, output);

        var content = output.Content.GetContent();
        content.Should().Contain("abc123-400w.avif");
        content.Should().Contain("abc123-800w.avif");
        content.Should().NotContain("abc123-320w.avif");
    }

    [Fact]
    public void Process_WithoutAssetId_DerivesFromSrcPath()
    {
        var tagHelper = new ResponsiveImageTagHelper
        {
            Src = "/assets/my-image-id.png",
            Alt = "Test",
            Priority = false,
            Responsive = true
        };

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("img");

        tagHelper.Process(context, output);

        var content = output.Content.GetContent();
        content.Should().Contain("my-image-id-320w.avif");
    }

    [Fact]
    public void Process_WithoutDimensions_OmitsWidthHeight()
    {
        var tagHelper = new ResponsiveImageTagHelper
        {
            Src = "/assets/abc123.jpg",
            AssetId = "abc123",
            Alt = "Test",
            Priority = false,
            Responsive = true
        };

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("img");

        tagHelper.Process(context, output);

        var content = output.Content.GetContent();
        content.Should().NotContain("width=");
        content.Should().NotContain("height=");
    }

    [Fact]
    public void Process_WhenNotResponsive_DoesNotModifyOutput()
    {
        var tagHelper = new ResponsiveImageTagHelper
        {
            Src = "/assets/abc123.jpg",
            Responsive = false
        };

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("img");

        tagHelper.Process(context, output);

        output.TagName.Should().Be("img");
        output.Content.GetContent().Should().BeEmpty();
    }

    private static TagHelperContext CreateTagHelperContext()
    {
        return new TagHelperContext(
            tagName: "img",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString());
    }

    private static TagHelperOutput CreateTagHelperOutput(string tagName)
    {
        return new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }
}
