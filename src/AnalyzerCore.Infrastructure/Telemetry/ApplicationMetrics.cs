using System.Diagnostics.Metrics;

namespace AnalyzerCore.Infrastructure.Telemetry;

/// <summary>
/// Application-level metrics for the Blockchain Analyzer.
/// Uses System.Diagnostics.Metrics which integrates with OpenTelemetry.
/// </summary>
public sealed class ApplicationMetrics : IDisposable
{
    public const string MeterName = "AnalyzerCore";

    private readonly Meter _meter;

    // Token metrics
    private readonly Counter<long> _tokensCreated;
    private readonly Counter<long> _tokensUpdated;
    private readonly ObservableGauge<long> _totalTokens;
    private long _totalTokensCount;

    // Pool metrics
    private readonly Counter<long> _poolsCreated;
    private readonly Counter<long> _poolReservesUpdated;
    private readonly ObservableGauge<long> _totalPools;
    private long _totalPoolsCount;

    // Blockchain RPC metrics
    private readonly Counter<long> _rpcCallsTotal;
    private readonly Counter<long> _rpcCallsErrors;
    private readonly Histogram<double> _rpcCallDuration;

    // Command/Query metrics
    private readonly Counter<long> _commandsProcessed;
    private readonly Counter<long> _queriesProcessed;
    private readonly Histogram<double> _commandProcessingDuration;
    private readonly Histogram<double> _queryProcessingDuration;

    // Outbox metrics
    private readonly Counter<long> _outboxMessagesCreated;
    private readonly Counter<long> _outboxMessagesProcessed;
    private readonly Counter<long> _outboxMessagesFailed;
    private readonly ObservableGauge<long> _outboxPendingMessages;
    private long _outboxPendingCount;

    public ApplicationMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        // Token metrics
        _tokensCreated = _meter.CreateCounter<long>(
            "analyzer_tokens_created_total",
            "tokens",
            "Total number of tokens created");

        _tokensUpdated = _meter.CreateCounter<long>(
            "analyzer_tokens_updated_total",
            "tokens",
            "Total number of token updates");

        _totalTokens = _meter.CreateObservableGauge(
            "analyzer_tokens_total",
            () => _totalTokensCount,
            "tokens",
            "Total number of tokens in the database");

        // Pool metrics
        _poolsCreated = _meter.CreateCounter<long>(
            "analyzer_pools_created_total",
            "pools",
            "Total number of pools created");

        _poolReservesUpdated = _meter.CreateCounter<long>(
            "analyzer_pool_reserves_updated_total",
            "updates",
            "Total number of pool reserve updates");

        _totalPools = _meter.CreateObservableGauge(
            "analyzer_pools_total",
            () => _totalPoolsCount,
            "pools",
            "Total number of pools in the database");

        // RPC metrics
        _rpcCallsTotal = _meter.CreateCounter<long>(
            "analyzer_rpc_calls_total",
            "calls",
            "Total number of blockchain RPC calls");

        _rpcCallsErrors = _meter.CreateCounter<long>(
            "analyzer_rpc_calls_errors_total",
            "errors",
            "Total number of failed blockchain RPC calls");

        _rpcCallDuration = _meter.CreateHistogram<double>(
            "analyzer_rpc_call_duration_seconds",
            "seconds",
            "Duration of blockchain RPC calls");

        // Command/Query metrics
        _commandsProcessed = _meter.CreateCounter<long>(
            "analyzer_commands_processed_total",
            "commands",
            "Total number of commands processed");

        _queriesProcessed = _meter.CreateCounter<long>(
            "analyzer_queries_processed_total",
            "queries",
            "Total number of queries processed");

        _commandProcessingDuration = _meter.CreateHistogram<double>(
            "analyzer_command_processing_duration_seconds",
            "seconds",
            "Duration of command processing");

        _queryProcessingDuration = _meter.CreateHistogram<double>(
            "analyzer_query_processing_duration_seconds",
            "seconds",
            "Duration of query processing");

        // Outbox metrics
        _outboxMessagesCreated = _meter.CreateCounter<long>(
            "analyzer_outbox_messages_created_total",
            "messages",
            "Total number of outbox messages created");

        _outboxMessagesProcessed = _meter.CreateCounter<long>(
            "analyzer_outbox_messages_processed_total",
            "messages",
            "Total number of outbox messages processed");

        _outboxMessagesFailed = _meter.CreateCounter<long>(
            "analyzer_outbox_messages_failed_total",
            "messages",
            "Total number of failed outbox messages");

        _outboxPendingMessages = _meter.CreateObservableGauge(
            "analyzer_outbox_pending_messages",
            () => _outboxPendingCount,
            "messages",
            "Number of pending outbox messages");
    }

    // Token operations
    public void RecordTokenCreated() => _tokensCreated.Add(1);
    public void RecordTokenUpdated() => _tokensUpdated.Add(1);
    public void SetTotalTokens(long count) => _totalTokensCount = count;

    // Pool operations
    public void RecordPoolCreated() => _poolsCreated.Add(1);
    public void RecordPoolReservesUpdated() => _poolReservesUpdated.Add(1);
    public void SetTotalPools(long count) => _totalPoolsCount = count;

    // RPC operations
    public void RecordRpcCall(string method) => _rpcCallsTotal.Add(1, new KeyValuePair<string, object?>("method", method));
    public void RecordRpcError(string method) => _rpcCallsErrors.Add(1, new KeyValuePair<string, object?>("method", method));
    public void RecordRpcDuration(double seconds, string method) => _rpcCallDuration.Record(seconds, new KeyValuePair<string, object?>("method", method));

    // Command/Query operations
    public void RecordCommandProcessed(string commandType) => _commandsProcessed.Add(1, new KeyValuePair<string, object?>("command", commandType));
    public void RecordQueryProcessed(string queryType) => _queriesProcessed.Add(1, new KeyValuePair<string, object?>("query", queryType));
    public void RecordCommandDuration(double seconds, string commandType) => _commandProcessingDuration.Record(seconds, new KeyValuePair<string, object?>("command", commandType));
    public void RecordQueryDuration(double seconds, string queryType) => _queryProcessingDuration.Record(seconds, new KeyValuePair<string, object?>("query", queryType));

    // Outbox operations
    public void RecordOutboxMessageCreated() => _outboxMessagesCreated.Add(1);
    public void RecordOutboxMessageProcessed() => _outboxMessagesProcessed.Add(1);
    public void RecordOutboxMessageFailed() => _outboxMessagesFailed.Add(1);
    public void SetOutboxPendingCount(long count) => _outboxPendingCount = count;

    public void Dispose() => _meter.Dispose();
}
