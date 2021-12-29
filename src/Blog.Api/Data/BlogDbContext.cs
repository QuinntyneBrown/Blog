using Blog.Api.Interfaces;
using Blog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Blog.Api.Data
{
    public class BlogDbContext : DbContext, IBlogDbContext
    {
        public DbSet<Post> Posts { get; private set; }
        public DbSet<User> Users { get; private set; }
        public DbSet<DigitalAsset> DigitalAssets { get; private set; }
        public DbSet<Content> Contents { get; private set; }
        public BlogDbContext(DbContextOptions options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlogDbContext).Assembly);
        }
    }
}
