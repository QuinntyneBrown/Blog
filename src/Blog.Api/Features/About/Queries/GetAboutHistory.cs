using Blog.Api.Common.Models;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.About.Queries;

public record AboutContentHistoryDto(
    Guid AboutContentHistoryId,
    string Heading,
    string Body,
    string BodyHtml,
    Guid? ProfileImageId,
    string? ProfileImageUrl,
    int Version,
    DateTime ArchivedAt);

public record GetAboutHistoryQuery(int Page, int PageSize) : IRequest<PagedResponse<AboutContentHistoryDto>>;

public class GetAboutHistoryQueryValidator : AbstractValidator<GetAboutHistoryQuery>
{
    public GetAboutHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public class GetAboutHistoryHandler(IUnitOfWork uow) : IRequestHandler<GetAboutHistoryQuery, PagedResponse<AboutContentHistoryDto>>
{
    public async Task<PagedResponse<AboutContentHistoryDto>> Handle(GetAboutHistoryQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await uow.AboutContents.GetHistoryAsync(request.Page, request.PageSize, cancellationToken);

        var dtos = new List<AboutContentHistoryDto>();
        foreach (var h in items)
        {
            string? imageUrl = null;
            if (h.ProfileImageId.HasValue)
            {
                var asset = await uow.DigitalAssets.GetByIdAsync(h.ProfileImageId.Value, cancellationToken);
                if (asset != null)
                    imageUrl = $"/assets/{asset.StoredFileName}";
            }

            dtos.Add(new AboutContentHistoryDto(
                h.AboutContentHistoryId,
                h.Heading,
                h.Body,
                h.BodyHtml,
                h.ProfileImageId,
                imageUrl,
                h.Version,
                h.ArchivedAt));
        }

        return new PagedResponse<AboutContentHistoryDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
