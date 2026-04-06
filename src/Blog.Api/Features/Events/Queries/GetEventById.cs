using Blog.Api.Common.Exceptions;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Events.Queries;

public record EventDto(
    Guid EventId,
    string Title,
    string Slug,
    string Description,
    DateTime StartDate,
    DateTime? EndDate,
    string TimeZoneId,
    DateTime StartDateUtc,
    DateTime? EndDateUtc,
    string Location,
    string? ExternalUrl,
    bool Published,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version)
{
    public static EventDto FromEntity(Event e) => new(
        e.EventId, e.Title, e.Slug, e.Description,
        e.StartDate, e.EndDate, e.TimeZoneId,
        e.StartDateUtc, e.EndDateUtc,
        e.Location, e.ExternalUrl,
        e.Published, e.CreatedAt, e.UpdatedAt, e.Version);
}

public record GetEventByIdQuery(Guid EventId) : IRequest<EventDto>;

public class GetEventByIdHandler(IEventRepository events) : IRequestHandler<GetEventByIdQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var ev = await events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException($"Event with ID '{request.EventId}' was not found.");

        return EventDto.FromEntity(ev);
    }
}
