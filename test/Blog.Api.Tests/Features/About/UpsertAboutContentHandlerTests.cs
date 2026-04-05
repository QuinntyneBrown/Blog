using Blog.Api.Features.About.Commands;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Blog.Api.Tests.Features.About;

public class UpsertAboutContentHandlerTests
{
    private readonly IUnitOfWork _uow;
    private readonly IMarkdownConverter _markdownConverter;
    private readonly UpsertAboutContentCommandHandler _handler;

    public UpsertAboutContentHandlerTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _markdownConverter = Substitute.For<IMarkdownConverter>();
        _handler = new UpsertAboutContentCommandHandler(_uow, _markdownConverter);
    }

    [Fact]
    public async Task Handle_CreatesNewContent_WhenNoneExists()
    {
        // Arrange
        _uow.AboutContents.GetAsync(Arg.Any<CancellationToken>())
            .Returns((AboutContent?)null);
        _markdownConverter.Convert("# Hello").Returns("<h1>Hello</h1>");

        var command = new UpsertAboutContentCommand("About Me", "# Hello", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Heading.Should().Be("About Me");
        result.Body.Should().Be("# Hello");
        result.BodyHtml.Should().Be("<h1>Hello</h1>");
        await _uow.AboutContents.Received(1).AddAsync(Arg.Any<AboutContent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesExistingContent_WhenContentExists()
    {
        // Arrange
        var existing = new AboutContent
        {
            AboutContentId = Guid.NewGuid(),
            Heading = "Old Heading",
            Body = "Old body",
            BodyHtml = "<p>Old body</p>",
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _uow.AboutContents.GetAsync(Arg.Any<CancellationToken>())
            .Returns(existing);
        _markdownConverter.Convert("# Updated").Returns("<h1>Updated</h1>");

        var command = new UpsertAboutContentCommand("New Heading", "# Updated", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Heading.Should().Be("New Heading");
        result.BodyHtml.Should().Be("<h1>Updated</h1>");
        _uow.AboutContents.Received(1).Update(existing);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _uow.AboutContents.DidNotReceive().AddAsync(Arg.Any<AboutContent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsProfileImageId_WhenProvided()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        _uow.AboutContents.GetAsync(Arg.Any<CancellationToken>())
            .Returns((AboutContent?)null);
        _markdownConverter.Convert("text").Returns("<p>text</p>");

        var command = new UpsertAboutContentCommand("About", "text", imageId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ProfileImageId.Should().Be(imageId);
    }
}
