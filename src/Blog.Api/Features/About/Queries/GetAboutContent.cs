using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.About.Queries;

public record AboutContentDto(
    Guid AboutContentId, string Heading, string Body, string BodyHtml,
    Guid? ProfileImageId, string? ProfileImageUrl,
    int Version, DateTime CreatedAt, DateTime UpdatedAt);

public record GetAboutContentQuery : IRequest<AboutContentDto?>;

public class GetAboutContentHandler(IAboutContentRepository aboutContents)
    : IRequestHandler<GetAboutContentQuery, AboutContentDto?>
{
    public async Task<AboutContentDto?> Handle(GetAboutContentQuery request, CancellationToken cancellationToken)
    {
        var content = await aboutContents.GetAsync(cancellationToken);
        if (content == null) return null;

        return new AboutContentDto(
            content.AboutContentId, content.Heading, content.Body, content.BodyHtml,
            content.ProfileImageId,
            content.ProfileImage != null ? $"/assets/{content.ProfileImage.StoredFileName}" : null,
            content.Version, content.CreatedAt, content.UpdatedAt);
    }
}
