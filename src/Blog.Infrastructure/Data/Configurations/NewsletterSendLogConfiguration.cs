using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class NewsletterSendLogConfiguration : IEntityTypeConfiguration<NewsletterSendLog>
{
    public void Configure(EntityTypeBuilder<NewsletterSendLog> builder)
    {
        builder.HasKey(l => l.NewsletterSendLogId);
        builder.Property(l => l.NewsletterSendLogId).ValueGeneratedOnAdd();
        builder.Property(l => l.RecipientIdempotencyKey).IsRequired().HasMaxLength(64);

        builder.HasIndex(l => new { l.NewsletterId, l.RecipientIdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UQ_NewsletterSendLog_Newsletter_Recipient");

        builder.HasOne(l => l.Newsletter)
            .WithMany()
            .HasForeignKey(l => l.NewsletterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Subscriber)
            .WithMany()
            .HasForeignKey(l => l.SubscriberId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
