using Blog.Api.Features.DigitalAssets.Queries;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Blog.Api.Tests.Features.DigitalAssets;

public class GetDigitalAssetsHandlerTests
{
    private readonly IDigitalAssetRepository _repository;
    private readonly GetDigitalAssetsHandler _handler;

    public GetDigitalAssetsHandlerTests()
    {
        _repository = Substitute.For<IDigitalAssetRepository>();
        _handler = new GetDigitalAssetsHandler(_repository);
    }

    [Fact]
    public async Task Handle_CallsGetByCreatedByWithQueryUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repository.GetByCreatedByAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAsset>());

        // Act
        await _handler.Handle(new GetDigitalAssetsQuery(userId), CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByCreatedByAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoAssetsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repository.GetByCreatedByAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAsset>());

        // Act
        var result = await _handler.Handle(new GetDigitalAssetsQuery(userId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DoesNotReturnAssetsOwnedByAnotherUser()
    {
        // Arrange
        var requestingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        // Simulate repository correctly filtering – only assets for requestingUserId are returned
        _repository.GetByCreatedByAsync(requestingUserId, Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAsset>());

        _repository.GetByCreatedByAsync(otherUserId, Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAsset>
            {
                new DigitalAsset
                {
                    DigitalAssetId = Guid.NewGuid(),
                    OriginalFileName = "other.jpg",
                    StoredFileName = "other-stored.jpg",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 1024,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = otherUserId
                }
            });

        // Act
        var result = await _handler.Handle(new GetDigitalAssetsQuery(requestingUserId), CancellationToken.None);

        // Assert: requesting user sees no assets (other user's asset is not returned)
        result.Should().BeEmpty();
        await _repository.DidNotReceive().GetByCreatedByAsync(otherUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsDtoFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var asset = new DigitalAsset
        {
            DigitalAssetId = assetId,
            OriginalFileName = "photo.jpg",
            StoredFileName = "photo-stored.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Width = 800,
            Height = 600,
            CreatedAt = createdAt,
            CreatedBy = userId
        };

        _repository.GetByCreatedByAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAsset> { asset });

        // Act
        var result = await _handler.Handle(new GetDigitalAssetsQuery(userId), CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var dto = result[0];
        dto.DigitalAssetId.Should().Be(assetId);
        dto.OriginalFileName.Should().Be("photo.jpg");
        dto.ContentType.Should().Be("image/jpeg");
        dto.FileSizeBytes.Should().Be(2048);
        dto.Width.Should().Be(800);
        dto.Height.Should().Be(600);
        dto.CreatedAt.Should().Be(createdAt);
        dto.Url.Should().Be("/assets/photo-stored.jpg");
    }
}
