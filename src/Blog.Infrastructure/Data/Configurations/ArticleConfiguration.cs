using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(a => a.ArticleId);
        builder.Property(a => a.ArticleId).ValueGeneratedOnAdd();
        builder.Property(a => a.Title).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Slug).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Abstract).IsRequired().HasMaxLength(512);
        builder.Property(a => a.Body).IsRequired();
        builder.Property(a => a.BodyHtml).IsRequired();
        builder.Property(a => a.ReadingTimeMinutes).HasDefaultValue(1);
        builder.Property(a => a.Version).HasDefaultValue(1);
        builder.Property(a => a.Published).HasDefaultValue(false);

        builder.HasIndex(a => a.Slug).IsUnique().HasDatabaseName("IX_Articles_Slug");
        builder.HasIndex(a => new { a.Published, a.DatePublished }).HasDatabaseName("IX_Articles_Published_DatePublished");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("IX_Articles_CreatedAt");

        builder.HasOne(a => a.FeaturedImage)
            .WithMany()
            .HasForeignKey(a => a.FeaturedImageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
