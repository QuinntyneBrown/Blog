using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Data.Repositories;

public class EventRepository(BlogDbContext context) : IEventRepository
{
    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await context.Events.FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

    public async Task<Event?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await context.Events.FirstOrDefaultAsync(e => e.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> GetAllCountAsync(CancellationToken cancellationToken = default)
        => await context.Events.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Event>> GetUpcomingAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Events
            .AsNoTracking()
            .Where(e => e.Published && e.StartDateUtc >= DateTime.UtcNow)
            .OrderBy(e => e.StartDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> GetTotalUpcomingCountAsync(CancellationToken cancellationToken = default)
        => await context.Events.CountAsync(e => e.Published && e.StartDateUtc >= DateTime.UtcNow, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetPastAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Events
            .AsNoTracking()
            .Where(e => e.Published && e.StartDateUtc < DateTime.UtcNow)
            .OrderByDescending(e => e.StartDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> GetTotalPastCountAsync(CancellationToken cancellationToken = default)
        => await context.Events.CountAsync(e => e.Published && e.StartDateUtc < DateTime.UtcNow, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
        => await context.Events.AnyAsync(e => e.Slug == slug && (excludeEventId == null || e.EventId != excludeEventId), cancellationToken);

    public async Task<(int MaxVersion, int Count, DateTime MaxUpdatedAt)> GetPublishedStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await context.Events
            .Where(e => e.Published)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                MaxVersion = g.Max(e => e.Version),
                Count = g.Count(),
                MaxUpdatedAt = g.Max(e => e.UpdatedAt)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stats != null
            ? (stats.MaxVersion, stats.Count, stats.MaxUpdatedAt)
            : (0, 0, DateTime.UtcNow);
    }

    public async Task AddAsync(Event ev, CancellationToken cancellationToken = default)
        => await context.Events.AddAsync(ev, cancellationToken);

    public void Update(Event ev) => context.Events.Update(ev);
    public void Remove(Event ev) => context.Events.Remove(ev);
}
