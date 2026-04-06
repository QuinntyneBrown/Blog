using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.About.Queries;

public record PublicAboutContentDto(
    string Heading,
    string BodyHtml,
    string? ProfileImageUrl);

public record AboutContentDto(
    Guid AboutContentId,
    string Heading,
    string Body,
    string BodyHtml,
    Guid? ProfileImageId,
    string? ProfileImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version);

public record GetAboutContentQuery : IRequest<PublicAboutContentDto?>;

public class GetAboutContentHandler(IAboutContentRepository aboutContents) : IRequestHandler<GetAboutContentQuery, PublicAboutContentDto?>
{
    public async Task<PublicAboutContentDto?> Handle(GetAboutContentQuery request, CancellationToken cancellationToken)
    {
        var about = await aboutContents.GetCurrentAsync(cancellationToken);
        if (about == null)
            return null;

        var imageUrl = about.ProfileImage != null ? $"/assets/{about.ProfileImage.StoredFileName}" : null;

        return new PublicAboutContentDto(
            about.Heading,
            about.BodyHtml,
            imageUrl);
    }
}
