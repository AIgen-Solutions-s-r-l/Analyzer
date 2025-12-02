using AnalyzerCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnalyzerCore.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ApiKey entity.
/// </summary>
public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.KeyHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(k => k.KeyPrefix)
            .IsRequired()
            .HasMaxLength(8);

        builder.HasIndex(k => k.KeyPrefix);

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(k => k.UserId);

        builder.Property(k => k.IsActive)
            .HasDefaultValue(true);

        // Ignore domain events - they are not persisted
        builder.Ignore(k => k.DomainEvents);
    }
}
