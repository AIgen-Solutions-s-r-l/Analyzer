using System.Text.Json;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Infrastructure.Persistence.Configurations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.Persistence;

/// <summary>
/// Application database context implementing Unit of Work pattern.
/// </summary>
public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly ILogger<ApplicationDbContext>? _logger;
    private readonly IPublisher? _publisher;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ILogger<ApplicationDbContext>? logger = null,
        IPublisher? publisher = null)
        : base(options)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<Pool> Pools => Set<Pool>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Address, e.ChainId }).IsUnique();

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(42);

            entity.Property(e => e.Symbol)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ChainId)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.IsPlaceholder)
                .HasDefaultValue(false);

            // Ignore domain events - they are not persisted
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<Pool>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Address, e.Factory }).IsUnique();

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(42);

            entity.Property(e => e.Factory)
                .IsRequired()
                .HasMaxLength(42);

            entity.Property(e => e.Reserve0)
                .HasPrecision(36, 18);

            entity.Property(e => e.Reserve1)
                .HasPrecision(36, 18);

            entity.HasOne(e => e.Token0)
                .WithMany()
                .HasForeignKey(e => e.Token0Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Token1)
                .WithMany()
                .HasForeignKey(e => e.Token1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events - they are not persisted
            entity.Ignore(e => e.DomainEvents);
        });

        // Apply OutboxMessage configuration
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        // Apply IdempotentRequest configuration
        modelBuilder.ApplyConfiguration(new IdempotentRequestConfiguration());

        // Apply User configuration
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // Apply ApiKey configuration
        modelBuilder.ApplyConfiguration(new ApiKeyConfiguration());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Collect domain events from tracked entities before saving
            var domainEvents = GetDomainEvents();

            // Convert domain events to outbox messages (transactional outbox pattern)
            AddOutboxMessages(domainEvents);

            // Save changes (including outbox messages)
            var result = await base.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    private List<IDomainEvent> GetDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();

        var aggregateRoots = ChangeTracker
            .Entries<AggregateRoot<int>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregateRoot in aggregateRoots)
        {
            domainEvents.AddRange(aggregateRoot.DomainEvents);
            aggregateRoot.ClearDomainEvents();
        }

        return domainEvents;
    }

    private void AddOutboxMessages(List<IDomainEvent> domainEvents)
    {
        if (!domainEvents.Any())
            return;

        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = new OutboxMessage(
                domainEvent.EventId,
                domainEvent.GetType().AssemblyQualifiedName!,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                domainEvent.OccurredOnUtc);

            OutboxMessages.Add(outboxMessage);

            _logger?.LogDebug(
                "Domain event {EventType} with ID {EventId} added to outbox",
                domainEvent.GetType().Name,
                domainEvent.EventId);
        }
    }
}
