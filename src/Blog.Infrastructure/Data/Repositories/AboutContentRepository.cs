using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Data.Repositories;

public class AboutContentRepository(BlogDbContext context) : IAboutContentRepository
{
    public async Task<AboutContent?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await context.AboutContents
            .Include(a => a.ProfileImage)
            .SingleOrDefaultAsync(a => a.AboutContentId == AboutContent.WellKnownId, cancellationToken);
    }

    public async Task AddAsync(AboutContent entity, CancellationToken cancellationToken = default)
    {
        await context.AboutContents.AddAsync(entity, cancellationToken);
    }

    public void Update(AboutContent entity)
    {
        context.AboutContents.Update(entity);
    }

    public async Task AddHistoryAsync(AboutContentHistory entity, CancellationToken cancellationToken = default)
    {
        await context.AboutContentHistories.AddAsync(entity, cancellationToken);
    }

    public async Task<(IReadOnlyList<AboutContentHistory> Items, int TotalCount)> GetHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.AboutContentHistories
            .Where(h => h.AboutContentId == AboutContent.WellKnownId)
            .OrderByDescending(h => h.ArchivedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<AboutContentHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default)
    {
        return await context.AboutContentHistories
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.AboutContentHistoryId == historyId, cancellationToken);
    }
}
