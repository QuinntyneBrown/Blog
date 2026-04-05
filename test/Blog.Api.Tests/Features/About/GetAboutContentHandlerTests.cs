using Blog.Api.Features.About.Queries;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Blog.Api.Tests.Features.About;

public class GetAboutContentHandlerTests
{
    private readonly IAboutContentRepository _repository;
    private readonly GetAboutContentHandler _handler;

    public GetAboutContentHandlerTests()
    {
        _repository = Substitute.For<IAboutContentRepository>();
        _handler = new GetAboutContentHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenNoAboutContentExists()
    {
        // Arrange
        _repository.GetAsync(Arg.Any<CancellationToken>())
            .Returns((AboutContent?)null);

        // Act
        var result = await _handler.Handle(new GetAboutContentQuery(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenAboutContentExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var content = new AboutContent
        {
            AboutContentId = id,
            Heading = "About Me",
            Body = "# Hello",
            BodyHtml = "<h1>Hello</h1>",
            ProfileImageId = null,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        _repository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(content);

        // Act
        var result = await _handler.Handle(new GetAboutContentQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AboutContentId.Should().Be(id);
        result.Heading.Should().Be("About Me");
        result.Body.Should().Be("# Hello");
        result.BodyHtml.Should().Be("<h1>Hello</h1>");
        result.ProfileImageUrl.Should().BeNull();
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task Handle_IncludesProfileImageUrl_WhenImageExists()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var content = new AboutContent
        {
            AboutContentId = Guid.NewGuid(),
            Heading = "About Me",
            Body = "text",
            BodyHtml = "<p>text</p>",
            ProfileImageId = imageId,
            ProfileImage = new DigitalAsset
            {
                DigitalAssetId = imageId,
                StoredFileName = "profile.webp",
                OriginalFileName = "profile.webp",
                ContentType = "image/webp",
                CreatedBy = Guid.NewGuid()
            },
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(content);

        // Act
        var result = await _handler.Handle(new GetAboutContentQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ProfileImageUrl.Should().Be("/assets/profile.webp");
    }
}
