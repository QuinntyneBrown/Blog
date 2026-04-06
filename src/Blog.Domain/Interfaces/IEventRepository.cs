using Blog.Domain.Entities;

namespace Blog.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Event?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetAllCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetUpcomingAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalUpcomingCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetPastAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalPastCountAsync(CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeEventId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Event ev, CancellationToken cancellationToken = default);
    void Update(Event ev);
    void Remove(Event ev);
}
