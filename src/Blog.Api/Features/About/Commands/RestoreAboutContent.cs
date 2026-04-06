using Blog.Api.Common.Exceptions;
using Blog.Api.Features.About.Queries;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Blog.Api.Features.About.Commands;

public record RestoreAboutContentCommand(Guid HistoryId, int CurrentVersion) : IRequest<RestoreAboutContentResponse>;

public record RestoreAboutContentResponse(
    Guid AboutContentId,
    string Heading,
    string Body,
    string BodyHtml,
    Guid? ProfileImageId,
    string? ProfileImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version,
    bool ProfileImageRestored);

public class RestoreAboutContentCommandValidator : AbstractValidator<RestoreAboutContentCommand>
{
    public RestoreAboutContentCommandValidator()
    {
        RuleFor(x => x.HistoryId).NotEmpty();
        RuleFor(x => x.CurrentVersion).GreaterThan(0);
    }
}

public class RestoreAboutContentCommandHandler(
    IUnitOfWork uow,
    IMarkdownConverter markdownConverter,
    ICacheInvalidator cacheInvalidator,
    ILogger<RestoreAboutContentCommandHandler> logger) : IRequestHandler<RestoreAboutContentCommand, RestoreAboutContentResponse>
{
    public async Task<RestoreAboutContentResponse> Handle(RestoreAboutContentCommand request, CancellationToken cancellationToken)
    {
        var historyRecord = await uow.AboutContents.GetHistoryByIdAsync(request.HistoryId, cancellationToken)
            ?? throw new NotFoundException($"History record '{request.HistoryId}' was not found.");

        if (historyRecord.AboutContentId != AboutContent.WellKnownId)
            throw new NotFoundException($"History record '{request.HistoryId}' was not found.");

        var current = await uow.AboutContents.GetCurrentAsync(cancellationToken)
            ?? throw new NotFoundException("About content not found.");

        if (current.Version != request.CurrentVersion)
            throw new ConflictException("The about content has been modified. Please refresh and try again.");

        bool profileImageRestored = true;
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // Snapshot current state
            await uow.AboutContents.AddHistoryAsync(new AboutContentHistory
            {
                AboutContentHistoryId = Guid.NewGuid(),
                AboutContentId = current.AboutContentId,
                Heading = current.Heading,
                Body = current.Body,
                BodyHtml = current.BodyHtml,
                ProfileImageId = current.ProfileImageId,
                Version = current.Version,
                ArchivedAt = DateTime.UtcNow
            }, cancellationToken);

            // Restore fields from history
            current.Heading = historyRecord.Heading;
            current.Body = historyRecord.Body;
            current.BodyHtml = markdownConverter.Convert(historyRecord.Body);

            profileImageRestored = true;
            if (historyRecord.ProfileImageId.HasValue)
            {
                var asset = await uow.DigitalAssets.GetByIdAsync(historyRecord.ProfileImageId.Value, cancellationToken);
                if (asset != null)
                {
                    current.ProfileImageId = historyRecord.ProfileImageId;
                }
                else
                {
                    current.ProfileImageId = null;
                    profileImageRestored = false;
                }
            }
            else
            {
                current.ProfileImageId = null;
            }

            current.UpdatedAt = DateTime.UtcNow;
            current.Version++;

            uow.AboutContents.Update(current);
            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        cacheInvalidator.InvalidateAbout();
        logger.LogInformation("About content restored from history {HistoryId} (version {Version})", request.HistoryId, current.Version);

        var imageUrl = current.ProfileImageId.HasValue
            ? (await uow.DigitalAssets.GetByIdAsync(current.ProfileImageId.Value, cancellationToken))
                is { } img ? $"/assets/{img.StoredFileName}" : null
            : null;

        return new RestoreAboutContentResponse(
            current.AboutContentId, current.Heading, current.Body, current.BodyHtml,
            current.ProfileImageId, imageUrl,
            current.CreatedAt, current.UpdatedAt, current.Version,
            profileImageRestored);
    }
}
