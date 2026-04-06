using Blog.Api.Common.Exceptions;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Events.Queries;

public record PublicEventWithCacheInfo(PublicEventDto Event, int Version, DateTime UpdatedAt);

public record GetEventBySlugQuery(string Slug) : IRequest<PublicEventWithCacheInfo>;

public class GetEventBySlugHandler(IEventRepository events) : IRequestHandler<GetEventBySlugQuery, PublicEventWithCacheInfo>
{
    public async Task<PublicEventWithCacheInfo> Handle(GetEventBySlugQuery request, CancellationToken cancellationToken)
    {
        var ev = await events.GetBySlugAsync(request.Slug, cancellationToken);

        if (ev == null || !ev.Published)
            throw new NotFoundException($"Event with slug '{request.Slug}' was not found.");

        return new PublicEventWithCacheInfo(PublicEventDto.FromEntity(ev), ev.Version, ev.UpdatedAt);
    }
}
