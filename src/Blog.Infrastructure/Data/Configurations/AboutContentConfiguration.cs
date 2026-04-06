using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class AboutContentConfiguration : IEntityTypeConfiguration<AboutContent>
{
    public void Configure(EntityTypeBuilder<AboutContent> builder)
    {
        builder.HasKey(a => a.AboutContentId);
        builder.Property(a => a.Heading).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Body).IsRequired();
        builder.Property(a => a.BodyHtml).IsRequired();
        builder.Property(a => a.Version).HasDefaultValue(1).IsConcurrencyToken();

        builder.HasOne(a => a.ProfileImage)
            .WithMany()
            .HasForeignKey(a => a.ProfileImageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
