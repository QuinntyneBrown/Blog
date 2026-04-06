using Blog.Api.Common.Models;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Events.Queries;

public record EventListDto(
    Guid EventId,
    string Title,
    string Slug,
    DateTime StartDate,
    string Location,
    string TimeZoneId,
    bool Published);

public record GetEventsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResponse<EventListDto>>;

public class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(50);
    }
}

public class GetEventsHandler(IEventRepository events) : IRequestHandler<GetEventsQuery, PagedResponse<EventListDto>>
{
    public async Task<PagedResponse<EventListDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var items = await events.GetAllAsync(request.Page, request.PageSize, cancellationToken);
        var total = await events.GetAllCountAsync(cancellationToken);

        return new PagedResponse<EventListDto>
        {
            Items = items.Select(e => new EventListDto(
                e.EventId, e.Title, e.Slug,
                e.StartDate, e.Location, e.TimeZoneId,
                e.Published)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
