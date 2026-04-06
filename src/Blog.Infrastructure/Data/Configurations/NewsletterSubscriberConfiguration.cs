using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.HasKey(s => s.SubscriberId);
        builder.Property(s => s.SubscriberId).ValueGeneratedOnAdd();
        builder.Property(s => s.Email).IsRequired().HasMaxLength(256);
        builder.Property(s => s.ConfirmationTokenHash).HasMaxLength(64);
        builder.Property(s => s.Confirmed).HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => s.Email).IsUnique().HasDatabaseName("IX_NewsletterSubscriber_Email");
        builder.HasIndex(s => s.IsActive).HasDatabaseName("IX_NewsletterSubscriber_IsActive");
    }
}
