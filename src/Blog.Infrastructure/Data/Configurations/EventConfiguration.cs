using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).ValueGeneratedOnAdd();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Slug).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.StartDate).IsRequired();
        builder.Property(e => e.TimeZoneId).IsRequired().HasMaxLength(64);
        builder.Property(e => e.StartDateUtc).IsRequired();
        builder.Property(e => e.Location).IsRequired().HasMaxLength(512);
        builder.Property(e => e.ExternalUrl).HasMaxLength(2048);
        builder.Property(e => e.Published).HasDefaultValue(false);
        builder.Property(e => e.Version).HasDefaultValue(1).IsConcurrencyToken();

        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("IX_Events_Slug");
        builder.HasIndex(e => new { e.Published, e.StartDateUtc }).HasDatabaseName("IX_Events_Published_StartDateUtc");
    }
}
