using Blog.Api.Common.Exceptions;
using Blog.Api.Features.About.Queries;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Blog.Api.Features.About.Commands;

public record UpsertAboutContentCommand(string Heading, string Body, Guid? ProfileImageId, int Version) : IRequest<AboutContentDto>;

public class UpsertAboutContentCommandValidator : AbstractValidator<UpsertAboutContentCommand>
{
    public UpsertAboutContentCommandValidator()
    {
        RuleFor(x => x.Heading).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.ProfileImageId)
            .Must(id => id != Guid.Empty)
            .When(x => x.ProfileImageId.HasValue)
            .WithMessage("ProfileImageId must not be empty.");
    }
}

public class UpsertAboutContentCommandHandler(
    IUnitOfWork uow,
    IMarkdownConverter markdownConverter,
    ICacheInvalidator cacheInvalidator,
    ILogger<UpsertAboutContentCommandHandler> logger) : IRequestHandler<UpsertAboutContentCommand, AboutContentDto>
{
    public async Task<AboutContentDto> Handle(UpsertAboutContentCommand request, CancellationToken cancellationToken)
    {
        if (request.ProfileImageId.HasValue)
        {
            var asset = await uow.DigitalAssets.GetByIdAsync(request.ProfileImageId.Value, cancellationToken)
                ?? throw new NotFoundException($"Digital asset '{request.ProfileImageId.Value}' was not found.");
        }

        var bodyHtml = markdownConverter.Convert(request.Body);
        var existing = await uow.AboutContents.GetCurrentAsync(cancellationToken);

        if (existing == null)
        {
            if (request.Version != 0)
                throw new BadRequestException("Version must be 0 for the first save.");

            var entity = new AboutContent
            {
                AboutContentId = AboutContent.WellKnownId,
                Heading = request.Heading,
                Body = request.Body,
                BodyHtml = bodyHtml,
                ProfileImageId = request.ProfileImageId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            try
            {
                await uow.AboutContents.AddAsync(entity, cancellationToken);
                await uow.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // First-insert concurrency: another request inserted first. Retry as update.
                return await RetryAsUpdate(request, bodyHtml, cancellationToken);
            }

            cacheInvalidator.InvalidateAbout();
            logger.LogInformation("About content created (version {Version})", entity.Version);

            var imageUrl = entity.ProfileImageId.HasValue
                ? await ResolveImageUrl(entity.ProfileImageId.Value, cancellationToken)
                : null;

            return new AboutContentDto(
                entity.AboutContentId, entity.Heading, entity.Body, entity.BodyHtml,
                entity.ProfileImageId, imageUrl,
                entity.CreatedAt, entity.UpdatedAt, entity.Version);
        }

        // Update path
        if (request.Version < 1)
            throw new BadRequestException("Version must be >= 1 for updates.");

        if (existing.Version != request.Version)
            throw new ConflictException("The about content has been modified. Please refresh and try again.");

        // Snapshot current state into history
        await uow.AboutContents.AddHistoryAsync(new AboutContentHistory
        {
            AboutContentHistoryId = Guid.NewGuid(),
            AboutContentId = existing.AboutContentId,
            Heading = existing.Heading,
            Body = existing.Body,
            BodyHtml = existing.BodyHtml,
            ProfileImageId = existing.ProfileImageId,
            Version = existing.Version,
            ArchivedAt = DateTime.UtcNow
        }, cancellationToken);

        existing.Heading = request.Heading;
        existing.Body = request.Body;
        existing.BodyHtml = bodyHtml;
        existing.ProfileImageId = request.ProfileImageId;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        uow.AboutContents.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateAbout();
        logger.LogInformation("About content updated (version {Version})", existing.Version);

        var updatedImageUrl = existing.ProfileImage != null
            ? $"/assets/{existing.ProfileImage.StoredFileName}"
            : null;

        return new AboutContentDto(
            existing.AboutContentId, existing.Heading, existing.Body, existing.BodyHtml,
            existing.ProfileImageId, updatedImageUrl,
            existing.CreatedAt, existing.UpdatedAt, existing.Version);
    }

    private async Task<AboutContentDto> RetryAsUpdate(UpsertAboutContentCommand request, string bodyHtml, CancellationToken cancellationToken)
    {
        var existing = await uow.AboutContents.GetCurrentAsync(cancellationToken)
            ?? throw new ConflictException("Concurrent insert detected but record not found on retry.");

        if (existing.Version != 1)
            throw new ConflictException("The about content has been modified. Please refresh and try again.");

        await uow.AboutContents.AddHistoryAsync(new AboutContentHistory
        {
            AboutContentHistoryId = Guid.NewGuid(),
            AboutContentId = existing.AboutContentId,
            Heading = existing.Heading,
            Body = existing.Body,
            BodyHtml = existing.BodyHtml,
            ProfileImageId = existing.ProfileImageId,
            Version = existing.Version,
            ArchivedAt = DateTime.UtcNow
        }, cancellationToken);

        existing.Heading = request.Heading;
        existing.Body = request.Body;
        existing.BodyHtml = bodyHtml;
        existing.ProfileImageId = request.ProfileImageId;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version++;

        uow.AboutContents.Update(existing);
        await uow.SaveChangesAsync(cancellationToken);

        cacheInvalidator.InvalidateAbout();
        logger.LogInformation("About content upserted via retry (version {Version})", existing.Version);

        var imageUrl = existing.ProfileImage != null
            ? $"/assets/{existing.ProfileImage.StoredFileName}"
            : null;

        return new AboutContentDto(
            existing.AboutContentId, existing.Heading, existing.Body, existing.BodyHtml,
            existing.ProfileImageId, imageUrl,
            existing.CreatedAt, existing.UpdatedAt, existing.Version);
    }

    private async Task<string?> ResolveImageUrl(Guid imageId, CancellationToken cancellationToken)
    {
        var asset = await uow.DigitalAssets.GetByIdAsync(imageId, cancellationToken);
        return asset != null ? $"/assets/{asset.StoredFileName}" : null;
    }
}
