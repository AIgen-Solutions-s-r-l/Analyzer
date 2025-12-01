using AnalyzerCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerCore.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OutboxMessage entity.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.OccurredOnUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedOnUtc);

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);

        // Index for efficient querying of unprocessed messages
        builder.HasIndex(x => x.ProcessedOnUtc)
            .HasFilter("[ProcessedOnUtc] IS NULL");

        // Index for retry processing
        builder.HasIndex(x => new { x.ProcessedOnUtc, x.RetryCount });
    }
}
