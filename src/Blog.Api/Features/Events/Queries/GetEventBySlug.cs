using Blog.Api.Common.Exceptions;
using Blog.Domain.Interfaces;
using MediatR;

namespace Blog.Api.Features.Events.Queries;

public record GetEventBySlugQuery(string Slug) : IRequest<PublicEventDto>;

public class GetEventBySlugHandler(IEventRepository events) : IRequestHandler<GetEventBySlugQuery, PublicEventDto>
{
    public async Task<PublicEventDto> Handle(GetEventBySlugQuery request, CancellationToken cancellationToken)
    {
        var ev = await events.GetBySlugAsync(request.Slug, cancellationToken);

        if (ev == null || !ev.Published)
            throw new NotFoundException($"Event with slug '{request.Slug}' was not found.");

        return PublicEventDto.FromEntity(ev);
    }
}
