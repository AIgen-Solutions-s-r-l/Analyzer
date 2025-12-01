using AnalyzerCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerCore.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for IdempotentRequest entity.
/// </summary>
public sealed class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
        builder.ToTable("IdempotentRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        // Index for cleanup of old requests
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
