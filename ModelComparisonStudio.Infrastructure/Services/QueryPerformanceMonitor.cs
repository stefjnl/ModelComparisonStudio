using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ModelComparisonStudio.Infrastructure.Services;

/// <summary>
/// Service for monitoring and tracking query performance metrics.
/// </summary>
public class QueryPerformanceMonitor
{
    private readonly ILogger<QueryPerformanceMonitor> _logger;
    private readonly Dictionary<string, QueryPerformanceStats> _stats = new();

    public QueryPerformanceMonitor(ILogger<QueryPerformanceMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts tracking a query execution.
    /// </summary>
    /// <param name="queryName">Name of the query being executed.</param>
    /// <returns>A disposable stopwatch that will record the execution time when disposed.</returns>
    public QueryExecutionTracker TrackQuery(string queryName)
    {
        return new QueryExecutionTracker(queryName, this);
    }

    /// <summary>
    /// Records the execution time for a query.
    /// </summary>
    /// <param name="queryName">Name of the query.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="resultCount">Number of results returned (optional).</param>
    public void RecordQueryExecution(string queryName, long executionTimeMs, int? resultCount = null)
    {
        lock (_stats)
        {
            if (!_stats.TryGetValue(queryName, out var stats))
            {
                stats = new QueryPerformanceStats(queryName);
                _stats[queryName] = stats;
            }

            stats.RecordExecution(executionTimeMs, resultCount);
        }

        _logger.LogDebug("Query {QueryName} executed in {ExecutionTimeMs}ms with {ResultCount} results",
            queryName, executionTimeMs, resultCount ?? 0);
    }

    /// <summary>
    /// Gets performance statistics for all tracked queries.
    /// </summary>
    /// <returns>Dictionary of query performance statistics.</returns>
    public IReadOnlyDictionary<string, QueryPerformanceStats> GetPerformanceStats()
    {
        lock (_stats)
        {
            return new Dictionary<string, QueryPerformanceStats>(_stats);
        }
    }

    /// <summary>
    /// Gets performance statistics for a specific query.
    /// </summary>
    /// <param name="queryName">Name of the query.</param>
    /// <returns>Performance statistics for the query, or null if not found.</returns>
    public QueryPerformanceStats? GetQueryStats(string queryName)
    {
        lock (_stats)
        {
            return _stats.TryGetValue(queryName, out var stats) ? stats : null;
        }
    }

    /// <summary>
    /// Resets all performance statistics.
    /// </summary>
    public void ResetStats()
    {
        lock (_stats)
        {
            _stats.Clear();
        }
        _logger.LogInformation("Query performance statistics reset");
    }

    /// <summary>
    /// Logs a summary of performance statistics.
    /// </summary>
    public void LogPerformanceSummary()
    {
        var stats = GetPerformanceStats();
        if (!stats.Any())
        {
            _logger.LogInformation("No query performance statistics available");
            return;
        }

        _logger.LogInformation("=== QUERY PERFORMANCE SUMMARY ===");
        foreach (var (queryName, queryStats) in stats.OrderByDescending(s => s.Value.AverageExecutionTimeMs))
        {
            _logger.LogInformation(
                "Query: {QueryName} | " +
                "Avg: {AverageTime:F2}ms | " +
                "Min: {MinTime}ms | " +
                "Max: {MaxTime}ms | " +
                "Count: {ExecutionCount} | " +
                "Avg Results: {AverageResults:F1}",
                queryName,
                queryStats.AverageExecutionTimeMs,
                queryStats.MinExecutionTimeMs,
                queryStats.MaxExecutionTimeMs,
                queryStats.ExecutionCount,
                queryStats.AverageResultCount);
        }
        _logger.LogInformation("=== END SUMMARY ===");
    }
}

/// <summary>
/// Performance statistics for a specific query.
/// </summary>
public class QueryPerformanceStats
{
    public string QueryName { get; }
    public long TotalExecutionTimeMs { get; private set; }
    public int ExecutionCount { get; private set; }
    public long MinExecutionTimeMs { get; private set; } = long.MaxValue;
    public long MaxExecutionTimeMs { get; private set; }
    public long TotalResultCount { get; private set; }
    public int ResultCountSamples { get; private set; }

    public double AverageExecutionTimeMs => ExecutionCount > 0 ? (double)TotalExecutionTimeMs / ExecutionCount : 0;
    public double AverageResultCount => ResultCountSamples > 0 ? (double)TotalResultCount / ResultCountSamples : 0;

    public QueryPerformanceStats(string queryName)
    {
        QueryName = queryName;
    }

    public void RecordExecution(long executionTimeMs, int? resultCount = null)
    {
        TotalExecutionTimeMs += executionTimeMs;
        ExecutionCount++;

        if (executionTimeMs < MinExecutionTimeMs)
            MinExecutionTimeMs = executionTimeMs;

        if (executionTimeMs > MaxExecutionTimeMs)
            MaxExecutionTimeMs = executionTimeMs;

        if (resultCount.HasValue)
        {
            TotalResultCount += resultCount.Value;
            ResultCountSamples++;
        }
    }
}

/// <summary>
/// Disposable tracker for query execution time.
/// </summary>
public class QueryExecutionTracker : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly string _queryName;
    private readonly QueryPerformanceMonitor _monitor;
    private bool _disposed = false;

    public QueryExecutionTracker(string queryName, QueryPerformanceMonitor monitor)
    {
        _queryName = queryName;
        _monitor = monitor;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _monitor.RecordQueryExecution(_queryName, _stopwatch.ElapsedMilliseconds);
            _disposed = true;
        }
    }

    /// <summary>
    /// Completes the tracking with a specific result count.
    /// </summary>
    /// <param name="resultCount">Number of results returned by the query.</param>
    public void CompleteWithResultCount(int resultCount)
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _monitor.RecordQueryExecution(_queryName, _stopwatch.ElapsedMilliseconds, resultCount);
            _disposed = true;
        }
    }
}
