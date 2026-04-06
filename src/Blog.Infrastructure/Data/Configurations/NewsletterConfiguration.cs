using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class NewsletterConfiguration : IEntityTypeConfiguration<Newsletter>
{
    public void Configure(EntityTypeBuilder<Newsletter> builder)
    {
        builder.HasKey(n => n.NewsletterId);
        builder.Property(n => n.NewsletterId).ValueGeneratedOnAdd();
        builder.Property(n => n.Subject).IsRequired().HasMaxLength(512);
        builder.Property(n => n.Slug).HasMaxLength(512);
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.BodyHtml).IsRequired();
        builder.Property(n => n.Status).HasDefaultValue(NewsletterStatus.Draft);
        builder.Property(n => n.Version).HasDefaultValue(1).IsConcurrencyToken();

        builder.HasIndex(n => n.Slug)
            .IsUnique()
            .HasFilter("[Slug] IS NOT NULL")
            .HasDatabaseName("IX_Newsletter_Slug");
        builder.HasIndex(n => n.Status).HasDatabaseName("IX_Newsletter_Status");
        builder.HasIndex(n => n.CreatedAt).HasDatabaseName("IX_Newsletter_CreatedAt");
    }
}
