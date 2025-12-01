# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A blockchain analysis tool built with Clean Architecture in .NET 6.0 for real-time monitoring and analysis of EVM-compatible blockchain activities. Uses Entity Framework Core with SQLite for persistence and Nethereum for blockchain interaction.

## Build and Run Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run the application
dotnet run --project src/AnalyzerCore.Api/AnalyzerCore.Api.csproj

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Apply database migrations
dotnet ef database update --project src/AnalyzerCore.Infrastructure --startup-project src/AnalyzerCore.Api

# Create new migration
dotnet ef migrations add <MigrationName> --project src/AnalyzerCore.Infrastructure --startup-project src/AnalyzerCore.Api
```

## Architecture

The solution follows Clean Architecture with four layers:

### Layer Dependencies
```
Api → Infrastructure → Application → Domain
                   ↘               ↗
                     → Domain ←
```

### AnalyzerCore.Domain
Core business logic with minimal external dependencies (only MediatR.Contracts for domain events):

**Abstractions** (`Abstractions/`):
- `Entity<TId>` - Base entity with identity-based equality
- `AggregateRoot<TId>` - Base for aggregates with domain events support
- `IDomainEvent` / `DomainEvent` - Domain event contracts
- `Result` / `Result<T>` - Railway-oriented error handling
- `Error` / `ValidationError` - Typed error representations
- `IUnitOfWork` - Unit of work abstraction

**Value Objects** (`ValueObjects/`):
- `EthereumAddress` - Validated Ethereum address with checksum support
- `ChainId` - Chain identifier with well-known chains (Ethereum, Polygon, BSC, etc.)
- `PoolType` - Pool type enumeration

**Entities** (`Entities/`):
- `Token` - ERC20 token with placeholder support, extends `AggregateRoot<int>`
- `Pool` - Liquidity pool with reserve tracking, extends `AggregateRoot<int>`

**Domain Events** (`Events/`):
- `PoolCreatedDomainEvent`, `PoolReservesUpdatedDomainEvent`
- `TokenCreatedDomainEvent`, `TokenInfoUpdatedDomainEvent`

**Domain Errors** (`Errors/DomainErrors.cs`):
- `DomainErrors.Address` - Address validation errors
- `DomainErrors.Token` - Token-related errors
- `DomainErrors.Pool` - Pool-related errors
- `DomainErrors.Blockchain` - RPC/blockchain errors

**Specifications** (`Specifications/`):
- `BaseSpecification<T>` - Base specification pattern implementation
- `PoolByAddressSpecification`, `PoolsByTokenSpecification`, etc.
- `TokenByAddressSpecification`, `TokensByChainIdSpecification`, etc.

### AnalyzerCore.Application
CQRS implementation with MediatR and FluentValidation:

**Abstractions** (`Abstractions/Messaging/`):
- `ICommand` / `ICommand<T>` - Command markers returning `Result`
- `IIdempotentCommand<T>` - Idempotent command with `RequestId`
- `IQuery<T>` - Query marker returning `Result<T>`
- `ICommandHandler<T>` / `IQueryHandler<T,R>` - Handler interfaces
- `IDomainEventHandler<T>` - Domain event handler interface
- `IIdempotencyService` - Service for tracking processed requests

**Caching** (`Abstractions/Caching/`):
- `ICacheService` - Cache abstraction with `GetOrSetAsync`
- `CacheKeys` - Centralized cache key management

**Pipeline Behaviors** (`Behaviors/`):
- `LoggingBehavior` - Structured logging with duration tracking
- `IdempotencyBehavior` - Prevents duplicate command processing
- `ValidationBehavior` - Automatic FluentValidation before handlers
- `UnitOfWorkBehavior` - Auto-commit after successful commands

**Commands**:
- `CreatePoolCommand` + `CreatePoolCommandValidator` + `CreatePoolCommandHandler`
- `CreateTokenCommand` + `CreateTokenCommandValidator` + `CreateTokenCommandHandler`
- `UpdatePoolReservesCommand` + `UpdatePoolReservesCommandValidator` + `UpdatePoolReservesCommandHandler`

**Queries**:
- `GetPoolByAddressQuery`, `GetPoolsByTokenQuery`
- `GetTokenByAddressQuery`, `GetTokensByChainIdQuery`

### AnalyzerCore.Infrastructure
External concerns with Options Pattern, Health Checks, Caching, and Outbox Pattern:

**Configuration** (`Configuration/`):
- `BlockchainOptions` - Chain configuration with validation
- `MonitoringOptions` - Monitoring parameters with validation
- `DatabaseOptions` - Database connection settings
- `CachingOptions` - Cache expiration settings
- `OutboxOptions` - Outbox processor configuration

**Persistence** (`Persistence/`):
- `ApplicationDbContext` - Implements `IUnitOfWork`, writes domain events to outbox
- `TokenRepository`, `PoolRepository` - Repository implementations
- `OutboxMessage` - Transactional outbox entity for reliable event delivery
- `IdempotentRequest` - Tracks processed commands for idempotency

**Caching** (`Caching/`):
- `InMemoryCacheService` - IMemoryCache-based implementation
- `CachedPoolRepository`, `CachedTokenRepository` - Caching decorators

**Blockchain** (`Blockchain/`):
- `BlockchainService` - Nethereum wrapper with Result-based safe methods
- `ERC20ABI`, `UniswapV2PairABI` - Contract ABI constants

**Health Checks** (`HealthChecks/`):
- `DatabaseHealthCheck` - SQLite connectivity check
- `BlockchainRpcHealthCheck` - RPC endpoint health with chain ID verification

**Background Services**:
- `BlockchainMonitorService` - Block scanning with Polly retry policies
- `OutboxProcessorService` - Processes outbox messages, publishes domain events

**Services** (`Services/`):
- `IdempotencyService` - EF Core-based idempotency tracking

### AnalyzerCore.Api
Application entry point:
- Clean `Program.cs` using `AddApplication()` and `AddInfrastructure()` extensions
- Serilog structured logging configuration

### Tests

**AnalyzerCore.Domain.Tests** - Unit tests with xUnit and FluentAssertions:
- `ResultTests` - Result pattern functionality
- `EthereumAddressTests` - Address validation and equality
- `ChainIdTests` - Chain ID validation and well-known chains

**AnalyzerCore.Architecture.Tests** - Architecture tests with NetArchTest:
- Layer dependency enforcement (Domain has no dependencies on other layers)
- Naming conventions (handlers, validators, specifications)
- Domain model rules (entities, value objects)

## Key Design Patterns

| Pattern | Implementation |
|---------|----------------|
| Result Pattern | `Result<T>` with `Map`, `Bind`, `Match` methods |
| Value Objects | `EthereumAddress`, `ChainId` with factory methods |
| Domain Events | Via `AggregateRoot` + Transactional Outbox |
| CQRS | `ICommand`/`IQuery` separation with Result returns |
| Unit of Work | `IUnitOfWork` + `UnitOfWorkBehavior` auto-commit |
| Specification | `ISpecification<T>` for composable queries |
| Pipeline Behaviors | Logging, Idempotency, Validation, UnitOfWork |
| Options Pattern | Typed configuration with DataAnnotations validation |
| Decorator Pattern | `CachedPoolRepository`, `CachedTokenRepository` |
| Transactional Outbox | `OutboxMessage` + `OutboxProcessorService` |
| Idempotency | `IIdempotentCommand` + `IdempotencyBehavior` |

## Entity Creation with Result Pattern

```csharp
// New way - with validation and Result
var addressResult = EthereumAddress.Create("0x...");
if (addressResult.IsFailure)
    return Result.Failure<Pool>(addressResult.Error);

var poolResult = Pool.Create(addressResult.Value, token0, token1, factory);
if (poolResult.IsFailure)
    return poolResult;

var pool = poolResult.Value;
pool.UpdateReserves(reserve0, reserve1); // Returns Result
```

## Configuration

Configuration in `src/AnalyzerCore.Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection` - SQLite database path
- `ChainConfig` - Chain ID, name, RPC URL, block time, confirmation blocks
- `Monitoring` - Polling interval, batch size, retry parameters
- `Caching` - Cache enable/disable, pool/token expiration times
- `Outbox` - Processing interval, batch size, max retries

Options are validated at startup using DataAnnotations.

## Health Checks

Available health check endpoints (when HTTP hosting is added):
- `database` - SQLite connectivity, token/pool counts
- `blockchain-rpc` - RPC connectivity, chain ID verification

## Dev-Prompts Orchestrator

The `dev-prompts/` directory contains a separate Python-based workflow orchestrator. See `dev-prompts/CLAUDE.md` for that subsystem's documentation.
