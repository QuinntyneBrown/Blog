using Blog.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Blog.Api.Interfaces
{
    public interface IBlogDbContext
    {
        DbSet<Post> Posts { get; }
        DbSet<User> Users { get; }
        DbSet<DigitalAsset> DigitalAssets { get; }
        DbSet<Content> Contents { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    }
}
