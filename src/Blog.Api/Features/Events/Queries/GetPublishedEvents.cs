using Blog.Api.Common.Models;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Blog.Api.Features.Events.Queries;

public record PublicEventDto(
    string Title,
    string Slug,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    string TimeZoneId,
    DateTime StartDateUtc,
    string Location,
    string? ExternalUrl)
{
    public static PublicEventDto FromEntity(Event e) => new(
        e.Title, e.Slug, e.Description,
        e.StartDate, e.EndDate, e.TimeZoneId,
        e.StartDateUtc, e.Location, e.ExternalUrl);
}

public record PublicEventsDto(
    PagedResponse<PublicEventDto> Upcoming,
    PagedResponse<PublicEventDto> Past);

public record GetPublishedEventsQuery(int UpcomingPage = 1, int PastPage = 1, int PageSize = 20) : IRequest<PublicEventsDto>;

public class GetPublishedEventsQueryValidator : AbstractValidator<GetPublishedEventsQuery>
{
    public GetPublishedEventsQueryValidator()
    {
        RuleFor(x => x.UpcomingPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PastPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(50);
    }
}

public class GetPublishedEventsHandler(IEventRepository events) : IRequestHandler<GetPublishedEventsQuery, PublicEventsDto>
{
    public async Task<PublicEventsDto> Handle(GetPublishedEventsQuery request, CancellationToken cancellationToken)
    {
        var upcomingItems = await events.GetUpcomingAsync(request.UpcomingPage, request.PageSize, cancellationToken);
        var upcomingTotal = await events.GetTotalUpcomingCountAsync(cancellationToken);

        var pastItems = await events.GetPastAsync(request.PastPage, request.PageSize, cancellationToken);
        var pastTotal = await events.GetTotalPastCountAsync(cancellationToken);

        var upcoming = new PagedResponse<PublicEventDto>
        {
            Items = upcomingItems.Select(PublicEventDto.FromEntity).ToList(),
            Page = request.UpcomingPage,
            PageSize = request.PageSize,
            TotalCount = upcomingTotal
        };

        var past = new PagedResponse<PublicEventDto>
        {
            Items = pastItems.Select(PublicEventDto.FromEntity).ToList(),
            Page = request.PastPage,
            PageSize = request.PageSize,
            TotalCount = pastTotal
        };

        return new PublicEventsDto(upcoming, past);
    }
}
