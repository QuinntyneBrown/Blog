using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.OutboxMessageId);
        builder.Property(o => o.OutboxMessageId).ValueGeneratedOnAdd();
        builder.Property(o => o.MessageType).IsRequired().HasMaxLength(128);
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.RetryCount).HasDefaultValue(0);
        builder.Property(o => o.Status).HasDefaultValue(OutboxMessageStatus.Pending);

        builder.HasIndex(o => new { o.Status, o.NextRetryAt })
            .HasDatabaseName("IX_OutboxMessage_Status_NextRetryAt");
    }
}
