using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes outbox messages and publishes domain events.
/// Part of the Transactional Outbox Pattern for reliable event publishing.
/// </summary>
public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly OutboxOptions _options;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger,
        IOptions<OutboxOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Outbox processor service stopping");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < _options.MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
        {
            return;
        }

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType is null)
                {
                    _logger.LogWarning(
                        "Unknown event type {EventType} for message {MessageId}",
                        message.Type,
                        message.Id);
                    message.MarkAsFailed($"Unknown event type: {message.Type}");
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as IDomainEvent;
                if (domainEvent is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize event {EventType} for message {MessageId}",
                        message.Type,
                        message.Id);
                    message.MarkAsFailed("Failed to deserialize domain event");
                    continue;
                }

                await publisher.Publish(domainEvent, cancellationToken);

                message.MarkAsProcessed(DateTime.UtcNow);

                _logger.LogDebug(
                    "Successfully processed outbox message {MessageId} of type {EventType}",
                    message.Id,
                    eventType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing outbox message {MessageId}",
                    message.Id);
                message.MarkAsFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
