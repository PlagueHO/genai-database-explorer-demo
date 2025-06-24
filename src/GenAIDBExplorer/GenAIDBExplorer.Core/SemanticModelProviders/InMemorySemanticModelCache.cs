using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

/// <summary>
/// In-memory implementation of semantic model caching with configurable TTL.
/// </summary>
public sealed class InMemorySemanticModelCache : ISemanticModelCache, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemorySemanticModelCache> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _cacheKeys = new();
    private long _totalRequests;
    private long _hits;
    private long _misses;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySemanticModelCache"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public InMemorySemanticModelCache(IMemoryCache memoryCache, ILogger<InMemorySemanticModelCache> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, TableInfo>> GetOrSetTablesAsync(string key, Func<Task<Dictionary<string, TableInfo>>> factory, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        return await GetOrSetAsync(key, factory, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, ViewInfo>> GetOrSetViewsAsync(string key, Func<Task<Dictionary<string, ViewInfo>>> factory, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        return await GetOrSetAsync(key, factory, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, StoredProcedureInfo>> GetOrSetStoredProceduresAsync(string key, Func<Task<Dictionary<string, StoredProcedureInfo>>> factory, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        return await GetOrSetAsync(key, factory, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SemanticModelColumn>> GetOrSetColumnsAsync(string key, Func<Task<List<SemanticModelColumn>>> factory, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        return await GetOrSetAsync(key, factory, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<Dictionary<string, object>>> GetOrSetSampleDataAsync(string key, Func<Task<List<Dictionary<string, object>>>> factory, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        return await GetOrSetAsync(key, factory, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> GetOrSetViewDefinitionAsync(string key, Func<Task<string>> factory, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        return await GetOrSetAsync(key, factory, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void InvalidateKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _memoryCache.Remove(key);
        _cacheKeys.TryRemove(key, out _);
        
        _logger.LogDebug("Cache key '{Key}' invalidated", key);
    }

    /// <inheritdoc/>
    public void InvalidatePattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var keysToRemove = _cacheKeys.Keys.Where(key => regex.IsMatch(key)).ToList();

        foreach (var key in keysToRemove)
        {
            InvalidateKey(key);
        }

        _logger.LogDebug("Cache pattern '{Pattern}' invalidated, removed {Count} keys", pattern, keysToRemove.Count);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        var keyCount = _cacheKeys.Count;
        
        foreach (var key in _cacheKeys.Keys.ToList())
        {
            _memoryCache.Remove(key);
        }
        
        _cacheKeys.Clear();
        
        _logger.LogDebug("Cache cleared, removed {Count} keys", keyCount);
    }

    /// <inheritdoc/>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalRequests = _totalRequests,
            Hits = _hits,
            Misses = _misses,
            CacheSize = _cacheKeys.Count
        };
    }

    /// <summary>
    /// Generic method to get or set cached data.
    /// </summary>
    /// <typeparam name="T">The type of data to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Function to execute if data is not cached.</param>
    /// <param name="ttl">Time-to-live for the cached data.</param>
    /// <returns>The cached or newly created data.</returns>
    private async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        Interlocked.Increment(ref _totalRequests);

        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue is not null)
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Cache hit for key '{Key}'", key);
            return cachedValue;
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache miss for key '{Key}', executing factory", key);

        var value = await factory().ConfigureAwait(false);
        
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.Normal
        };

        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            if (evictedKey is string keyString)
            {
                _cacheKeys.TryRemove(keyString, out _);
                _logger.LogDebug("Cache key '{Key}' evicted, reason: {Reason}", keyString, reason);
            }
        });

        _memoryCache.Set(key, value, options);
        _cacheKeys[key] = DateTime.UtcNow.Add(ttl);

        _logger.LogDebug("Cache set for key '{Key}' with TTL {TTL}", key, ttl);

        return value;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="InMemorySemanticModelCache"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cacheKeys.Clear();
            _disposed = true;
        }
    }
}