using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blog.Infrastructure.Data.Repositories;

public class AboutContentRepository(BlogDbContext context) : IAboutContentRepository
{
    public async Task<AboutContent?> GetAsync(CancellationToken cancellationToken = default)
        => await context.AboutContents.Include(a => a.ProfileImage)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(AboutContent aboutContent, CancellationToken cancellationToken = default)
        => await context.AboutContents.AddAsync(aboutContent, cancellationToken);

    public void Update(AboutContent aboutContent) => context.AboutContents.Update(aboutContent);
}
