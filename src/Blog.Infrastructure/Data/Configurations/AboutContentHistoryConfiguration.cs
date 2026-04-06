using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class AboutContentHistoryConfiguration : IEntityTypeConfiguration<AboutContentHistory>
{
    public void Configure(EntityTypeBuilder<AboutContentHistory> builder)
    {
        builder.HasKey(h => h.AboutContentHistoryId);
        builder.Property(h => h.AboutContentHistoryId).ValueGeneratedOnAdd();
        builder.Property(h => h.Heading).IsRequired().HasMaxLength(256);
        builder.Property(h => h.Body).IsRequired();
        builder.Property(h => h.BodyHtml).IsRequired();

        builder.HasOne<AboutContent>()
            .WithMany()
            .HasForeignKey(h => h.AboutContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.AboutContentId, h.ArchivedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_AboutContentHistory_AboutContentId_ArchivedAt");
    }
}
