using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

/// <summary>
/// Defines the contract for caching semantic model data with configurable TTL (Time-To-Live).
/// </summary>
public interface ISemanticModelCache
{
    /// <summary>
    /// Gets cached tables or executes the provided factory function if not cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created table data.</returns>
    Task<Dictionary<string, TableInfo>> GetOrSetTablesAsync(string key, Func<Task<Dictionary<string, TableInfo>>> factory, TimeSpan ttl);

    /// <summary>
    /// Gets cached views or executes the provided factory function if not cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created view data.</returns>
    Task<Dictionary<string, ViewInfo>> GetOrSetViewsAsync(string key, Func<Task<Dictionary<string, ViewInfo>>> factory, TimeSpan ttl);

    /// <summary>
    /// Gets cached stored procedures or executes the provided factory function if not cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created stored procedure data.</returns>
    Task<Dictionary<string, StoredProcedureInfo>> GetOrSetStoredProceduresAsync(string key, Func<Task<Dictionary<string, StoredProcedureInfo>>> factory, TimeSpan ttl);

    /// <summary>
    /// Gets cached columns for a table or executes the provided factory function if not cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created column data.</returns>
    Task<List<SemanticModelColumn>> GetOrSetColumnsAsync(string key, Func<Task<List<SemanticModelColumn>>> factory, TimeSpan ttl);

    /// <summary>
    /// Gets cached sample data or executes the provided factory function if not cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created sample data.</returns>
    Task<List<Dictionary<string, object>>> GetOrSetSampleDataAsync(string key, Func<Task<List<Dictionary<string, object>>>> factory, TimeSpan ttl);

    /// <summary>
    /// Gets cached view definition or executes the provided factory function if not cached.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created view definition.</returns>
    Task<string> GetOrSetViewDefinitionAsync(string key, Func<Task<string>> factory, TimeSpan ttl);

    /// <summary>
    /// Invalidates cached data for a specific key.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    void InvalidateKey(string key);

    /// <summary>
    /// Invalidates all cached data matching a pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys for invalidation.</param>
    void InvalidatePattern(string pattern);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets cache statistics including hit/miss ratios and cache size.
    /// </summary>
    /// <returns>Cache statistics.</returns>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Represents cache statistics and monitoring information.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of cache requests.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits.
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses.
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Gets the cache hit ratio as a percentage.
    /// </summary>
    public double HitRatio => TotalRequests > 0 ? (double)Hits / TotalRequests * 100 : 0;

    /// <summary>
    /// Gets or sets the current number of cached items.
    /// </summary>
    public int CacheSize { get; set; }
}