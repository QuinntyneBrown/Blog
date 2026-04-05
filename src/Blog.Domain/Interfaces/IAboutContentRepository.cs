using Blog.Domain.Entities;

namespace Blog.Domain.Interfaces;

public interface IAboutContentRepository
{
    Task<AboutContent?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AboutContent aboutContent, CancellationToken cancellationToken = default);
    void Update(AboutContent aboutContent);
}
