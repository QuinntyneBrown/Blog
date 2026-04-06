using Blog.Api.Common.Exceptions;
using Blog.Api.Common.Models;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Subscriptions.Queries;

public record GetSubscribersQuery(int Page = 1, int PageSize = 20, string? Status = null) : IRequest<PagedResponse<SubscriberDto>>;

public class GetSubscribersHandler(INewsletterRepository newsletters) : IRequestHandler<GetSubscribersQuery, PagedResponse<SubscriberDto>>
{
    private static readonly string[] ValidStatuses = ["confirmed", "unconfirmed", "inactive"];

    public async Task<PagedResponse<SubscriberDto>> Handle(GetSubscribersQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Status) && !ValidStatuses.Contains(request.Status.ToLowerInvariant()))
            throw new BadRequestException($"Invalid status value '{request.Status}'. Valid values are: confirmed, unconfirmed, inactive.");

        var items = await newsletters.GetSubscribersAsync(request.Page, request.PageSize, request.Status, cancellationToken);
        var total = await newsletters.GetSubscribersCountAsync(request.Status, cancellationToken);

        return new PagedResponse<SubscriberDto>
        {
            Items = items.Select(s => new SubscriberDto(
                s.SubscriberId, s.Email, s.Confirmed, s.IsActive,
                s.ConfirmedAt, s.ResubscribedAt, s.CreatedAt, s.UpdatedAt)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
