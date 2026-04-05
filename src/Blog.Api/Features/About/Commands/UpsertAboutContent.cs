using Blog.Api.Features.About.Queries;
using Blog.Api.Services;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.About.Commands;

public record UpsertAboutContentCommand(string Heading, string Body, Guid? ProfileImageId)
    : IRequest<AboutContentDto>;

public class UpsertAboutContentCommandValidator : AbstractValidator<UpsertAboutContentCommand>
{
    public UpsertAboutContentCommandValidator()
    {
        RuleFor(x => x.Heading).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class UpsertAboutContentCommandHandler(
    IUnitOfWork uow,
    IMarkdownConverter markdownConverter) : IRequestHandler<UpsertAboutContentCommand, AboutContentDto>
{
    public async Task<AboutContentDto> Handle(UpsertAboutContentCommand request, CancellationToken cancellationToken)
    {
        var bodyHtml = markdownConverter.Convert(request.Body);
        var existing = await uow.AboutContents.GetAsync(cancellationToken);

        if (existing != null)
        {
            existing.Heading = request.Heading;
            existing.Body = request.Body;
            existing.BodyHtml = bodyHtml;
            existing.ProfileImageId = request.ProfileImageId;
            uow.AboutContents.Update(existing);
        }
        else
        {
            existing = new AboutContent
            {
                AboutContentId = Guid.NewGuid(),
                Heading = request.Heading,
                Body = request.Body,
                BodyHtml = bodyHtml,
                ProfileImageId = request.ProfileImageId,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await uow.AboutContents.AddAsync(existing, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);

        return new AboutContentDto(
            existing.AboutContentId, existing.Heading, existing.Body, existing.BodyHtml,
            existing.ProfileImageId,
            existing.ProfileImage != null ? $"/assets/{existing.ProfileImage.StoredFileName}" : null,
            existing.Version, existing.CreatedAt, existing.UpdatedAt);
    }
}
