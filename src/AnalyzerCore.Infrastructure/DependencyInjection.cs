using AnalyzerCore.Application.Abstractions;
using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Models;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Infrastructure.BackgroundServices;
using AnalyzerCore.Infrastructure.Blockchain;
using AnalyzerCore.Infrastructure.Caching;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.Correlation;
using AnalyzerCore.Infrastructure.HealthChecks;
using AnalyzerCore.Infrastructure.Persistence;
using AnalyzerCore.Infrastructure.Persistence.Repositories;
using AnalyzerCore.Infrastructure.RateLimiting;
using AnalyzerCore.Infrastructure.Repositories;
using AnalyzerCore.Infrastructure.Services;
using AnalyzerCore.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nethereum.Web3;

namespace AnalyzerCore.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Options with validation
        services
            .AddOptions<BlockchainOptions>()
            .Bind(configuration.GetSection(BlockchainOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<CachingOptions>()
            .Bind(configuration.GetSection(CachingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Legacy ChainConfig support (for backward compatibility)
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlockchainOptions>>().Value;
            return new ChainConfig
            {
                ChainId = options.ChainId,
                Name = options.Name,
                RpcUrl = options.RpcUrl,
                RpcPort = options.RpcPort,
                BlockTime = options.BlockTime,
                ConfirmationBlocks = options.ConfirmationBlocks
            };
        });

        // Database
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlite(dbOptions.DefaultConnection, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(dbOptions.CommandTimeout);
            });

            if (dbOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register DbContext as IUnitOfWork
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Correlation ID accessor (scoped to share across the request)
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        // Idempotency Service
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // Blockchain
        services.AddSingleton<Web3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlockchainOptions>>().Value;
            return new Web3(options.GetFullRpcUrl());
        });

        // Rate Limiter for RPC calls
        services.AddSingleton<IRpcRateLimiter, SlidingWindowRateLimiter>();

        // Blockchain Service with optional rate limiting decorator
        services.AddScoped<BlockchainService>();
        services.AddScoped<IBlockchainService>(sp =>
        {
            var rateLimitOptions = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
                ?? new RateLimitOptions();

            var innerService = sp.GetRequiredService<BlockchainService>();

            if (!rateLimitOptions.Enabled)
            {
                return innerService;
            }

            return new RateLimitedBlockchainService(
                innerService,
                sp.GetRequiredService<IRpcRateLimiter>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RateLimitedBlockchainService>>(),
                sp.GetService<ApplicationMetrics>());
        });

        // Caching
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, InMemoryCacheService>();

        // Repositories (with caching decorator)
        services.AddScoped<TokenRepository>();
        services.AddScoped<PoolRepository>();

        services.AddScoped<ITokenRepository>(sp =>
        {
            var cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
            var innerRepository = sp.GetRequiredService<TokenRepository>();

            if (!cachingOptions.Enabled)
            {
                return innerRepository;
            }

            return new CachedTokenRepository(
                innerRepository,
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<IOptions<CachingOptions>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedTokenRepository>>());
        });

        services.AddScoped<IPoolRepository>(sp =>
        {
            var cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
            var innerRepository = sp.GetRequiredService<PoolRepository>();

            if (!cachingOptions.Enabled)
            {
                return innerRepository;
            }

            return new CachedPoolRepository(
                innerRepository,
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<IOptions<CachingOptions>>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedPoolRepository>>());
        });

        // Background Services
        services.AddHostedService<BlockchainMonitorService>();

        // Outbox processor (if enabled)
        var outboxOptions = configuration.GetSection(OutboxOptions.SectionName).Get<OutboxOptions>() ?? new OutboxOptions();
        if (outboxOptions.Enabled)
        {
            services.AddHostedService<OutboxProcessorService>();
        }

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "sql" })
            .AddCheck<BlockchainRpcHealthCheck>("blockchain-rpc", tags: new[] { "rpc", "blockchain" });

        return services;
    }
}
