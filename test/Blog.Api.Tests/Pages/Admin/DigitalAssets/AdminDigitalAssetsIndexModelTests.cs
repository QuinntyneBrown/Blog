using Blog.Api.Common.Exceptions;
using Blog.Api.Features.DigitalAssets.Commands;
using Blog.Api.Features.DigitalAssets.Queries;
using Blog.Api.Pages.Admin.DigitalAssets;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Claims;
using Xunit;

namespace Blog.Api.Tests.Pages.Admin.DigitalAssets;

public class AdminDigitalAssetsIndexModelTests
{
    private readonly IMediator _mediator;
    private readonly AdminDigitalAssetsIndexModel _pageModel;

    public AdminDigitalAssetsIndexModelTests()
    {
        _mediator = Substitute.For<IMediator>();
        _pageModel = new AdminDigitalAssetsIndexModel(_mediator);
    }

    [Fact]
    public async Task OnGetAsync_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var context = CreateHttpContext();
        // No jwt_token or jwt_expires set – unauthenticated
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = await _pageModel.OnGetAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Login");
    }

    [Fact]
    public async Task OnGetAsync_WhenAuthenticated_SendsQueryWithCurrentUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);

        _mediator.Send(Arg.Any<GetDigitalAssetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAssetDto>());

        // Act
        var result = await _pageModel.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        await _mediator.Received(1).Send(
            Arg.Is<GetDigitalAssetsQuery>(q => q.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnGetAsync_WhenAuthenticated_PopulatesAssetsFromQuery()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);

        var assetId = Guid.NewGuid();
        var expectedAssets = new List<DigitalAssetDto>
        {
            new DigitalAssetDto(assetId, "photo.jpg", "image/jpeg", 2048, 800, 600,
                "/assets/photo-stored.jpg", DateTime.UtcNow)
        };

        _mediator.Send(Arg.Any<GetDigitalAssetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedAssets);

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Assets.Should().HaveCount(1);
        _pageModel.Assets[0].DigitalAssetId.Should().Be(assetId);
        _pageModel.Assets[0].OriginalFileName.Should().Be("photo.jpg");
    }

    [Fact]
    public async Task OnGetAsync_DoesNotSendQueryForOtherUserId()
    {
        // Arrange
        var actualUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(actualUserId);
        _pageModel.PageContext = CreatePageContext(context);

        _mediator.Send(Arg.Any<GetDigitalAssetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<DigitalAssetDto>());

        // Act
        await _pageModel.OnGetAsync();

        // Assert: query was NOT sent with the other user's ID
        await _mediator.DidNotReceive().Send(
            Arg.Is<GetDigitalAssetsQuery>(q => q.UserId == otherUserId),
            Arg.Any<CancellationToken>());
    }

    // ─── OnPostAsync (upload) ───────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAsync_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var context = CreateHttpContext();
        _pageModel.PageContext = CreatePageContext(context);
        var file = Substitute.For<IFormFile>();

        // Act
        var result = await _pageModel.OnPostAsync(file);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Login");
    }

    [Fact]
    public async Task OnPostAsync_WhenAuthenticated_SendsUploadCommandWithCurrentUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);
        var file = Substitute.For<IFormFile>();

        _mediator.Send(Arg.Any<UploadDigitalAssetCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DigitalAssetDto(Guid.NewGuid(), "photo.jpg", "image/jpeg", 1024, 800, 600,
                "/assets/photo.jpg", DateTime.UtcNow));

        // Act
        var result = await _pageModel.OnPostAsync(file);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<UploadDigitalAssetCommand>(c => c.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnPostAsync_WhenUploadSucceeds_RedirectsWithSuccessMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);
        var file = Substitute.For<IFormFile>();

        _mediator.Send(Arg.Any<UploadDigitalAssetCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DigitalAssetDto(Guid.NewGuid(), "photo.jpg", "image/jpeg", 1024, 800, 600,
                "/assets/photo.jpg", DateTime.UtcNow));

        // Act
        var result = await _pageModel.OnPostAsync(file);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues.Should().ContainKey("success");
        redirect.RouteValues!["success"].Should().Be("Asset uploaded.");
    }

    [Fact]
    public async Task OnPostAsync_WhenUploadFails_RedirectsWithErrorMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);
        var file = Substitute.For<IFormFile>();
        var errorMessage = "File size exceeds the 10 MB limit.";

        _mediator.Send(Arg.Any<UploadDigitalAssetCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Act
        var result = await _pageModel.OnPostAsync(file);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues.Should().ContainKey("error");
        redirect.RouteValues!["error"].Should().Be(errorMessage);
    }

    [Fact]
    public async Task OnPostAsync_DoesNotShortCircuitWhenUserIdIsEmpty()
    {
        // Arrange: authenticated session but no claims populated (Guid.Empty scenario)
        var context = CreateHttpContext();
        context.Session.SetString("jwt_token", "valid.jwt.token");
        context.Session.SetString("jwt_expires", DateTime.UtcNow.AddMinutes(30).ToString("O"));
        // No ClaimsPrincipal set — GetCurrentUserId() will return Guid.Empty
        _pageModel.PageContext = CreatePageContext(context);
        var file = Substitute.For<IFormFile>();

        _mediator.Send(Arg.Any<UploadDigitalAssetCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DigitalAssetDto(Guid.NewGuid(), "photo.jpg", "image/jpeg", 1024, 800, 600,
                "/assets/photo.jpg", DateTime.UtcNow));

        // Act
        var result = await _pageModel.OnPostAsync(file);

        // Assert: should NOT redirect to login — upload proceeds even with Guid.Empty userId
        result.Should().BeOfType<RedirectToPageResult>()
            .Which.PageName.Should().NotBe("/Admin/Login");
    }

    // ─── OnPostDeleteAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task OnPostDeleteAsync_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var context = CreateHttpContext();
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = await _pageModel.OnPostDeleteAsync(Guid.NewGuid());

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Login");
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenAuthenticated_SendsDeleteCommandWithId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        await _pageModel.OnPostDeleteAsync(assetId);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<DeleteDigitalAssetCommand>(c => c.Id == assetId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenAuthenticated_RedirectsWithSuccessMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);

        // Act
        var result = await _pageModel.OnPostDeleteAsync(Guid.NewGuid());

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues.Should().ContainKey("success");
        redirect.RouteValues!["success"].Should().Be("Asset deleted.");
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenDeleteThrowsConflictException_RedirectsWithErrorMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var context = CreateAuthenticatedHttpContext(userId);
        _pageModel.PageContext = CreatePageContext(context);
        var errorMessage = "Cannot delete this asset because it is referenced by one or more articles.";

        _mediator.Send(Arg.Any<DeleteDigitalAssetCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException(errorMessage));

        // Act
        var result = await _pageModel.OnPostDeleteAsync(assetId);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues.Should().ContainKey("error");
        redirect.RouteValues!["error"].Should().Be(errorMessage);
    }

    // ─── helpers ────────────────────────────────────────────────────────────────

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var sessionFeature = new IndexTestSessionFeature();
        context.Features.Set<ISessionFeature>(sessionFeature);
        return context;
    }

    private static DefaultHttpContext CreateAuthenticatedHttpContext(Guid userId)
    {
        var context = CreateHttpContext();
        // Set a valid non-expired session token so IsAuthenticated() returns true
        context.Session.SetString("jwt_token", "valid.jwt.token");
        context.Session.SetString("jwt_expires", DateTime.UtcNow.AddMinutes(30).ToString("O"));
        // Populate User claims so GetCurrentUserId() returns the real userId
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "jwt"));
        return context;
    }

    private static PageContext CreatePageContext(HttpContext httpContext)
    {
        return new PageContext(new ActionContext(
            httpContext, new RouteData(), new CompiledPageActionDescriptor()));
    }
}

internal class IndexTestSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = new IndexTestSession();
}

internal class IndexTestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public string Id => "test-session-id";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        value = Array.Empty<byte>();
        return false;
    }
}
