# Semantic Model Caching

This document describes the caching strategy implemented for the GenAI Database Explorer to improve performance by reducing redundant database queries.

## Overview

The caching system provides a configurable TTL (Time-To-Live) based cache for semantic model data operations. It uses a decorator pattern to transparently add caching capabilities to the existing `ISchemaRepository` implementation without breaking existing code.

## Architecture

### Core Components

1. **ISemanticModelCache** - Interface defining cache operations with type-safe methods
2. **InMemorySemanticModelCache** - Thread-safe in-memory cache implementation
3. **CachedSchemaRepository** - Decorator that adds caching to any `ISchemaRepository`
4. **SemanticModelCacheSettings** - Configuration settings for cache TTL and behavior

### Cache Data Types

The cache supports the following data types with configurable TTL:

- **Tables** (default: 30 minutes)
- **Views** (default: 30 minutes) 
- **Stored Procedures** (default: 30 minutes)
- **Columns** (default: 60 minutes)
- **Sample Data** (default: 15 minutes)
- **View Definitions** (default: 60 minutes)

## Configuration

### Basic Configuration

Add the following to your `settings.json`:

```json
{
  "SemanticModel": {
    "MaxDegreeOfParallelism": 1,
    "Cache": {
      "Enabled": true,
      "TablesTtlMinutes": 30,
      "ViewsTtlMinutes": 30,
      "StoredProceduresTtlMinutes": 30,
      "ColumnsTtlMinutes": 60,
      "SampleDataTtlMinutes": 15,
      "ViewDefinitionsTtlMinutes": 60
    }
  }
}
```

### Cache Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Whether caching is enabled |
| `TablesTtlMinutes` | `30` | TTL for cached table metadata |
| `ViewsTtlMinutes` | `30` | TTL for cached view metadata |
| `StoredProceduresTtlMinutes` | `30` | TTL for cached stored procedure metadata |
| `ColumnsTtlMinutes` | `60` | TTL for cached column information |
| `SampleDataTtlMinutes` | `15` | TTL for cached sample data |
| `ViewDefinitionsTtlMinutes` | `60` | TTL for cached view definitions |

## Dependency Injection Setup

### Basic Setup

```csharp
services.AddSemanticModelCaching();
```

### With Custom Memory Cache Options

```csharp
services.AddSemanticModelCaching(options =>
{
    options.SizeLimit = 1000;
    options.CompactionPercentage = 0.25;
});
```

### Manual Registration

```csharp
services.AddMemoryCache();
services.AddSingleton<ISemanticModelCache, InMemorySemanticModelCache>();
services.Decorate<ISchemaRepository, CachedSchemaRepository>();
```

## Cache Operations

### Cache Statistics

Monitor cache performance using the statistics API:

```csharp
var cache = serviceProvider.GetService<ISemanticModelCache>();
var stats = cache.GetStatistics();

Console.WriteLine($"Hit Ratio: {stats.HitRatio:F2}%");
Console.WriteLine($"Total Requests: {stats.TotalRequests}");
Console.WriteLine($"Cache Size: {stats.CacheSize}");
```

### Cache Invalidation

#### Invalidate Specific Key
```csharp
cache.InvalidateKey("tables:dbo");
```

#### Invalidate by Pattern
```csharp
cache.InvalidatePattern("tables:.*");  // All table caches
cache.InvalidatePattern(".*:dbo.*");   // All dbo schema caches
```

#### Clear All Cache
```csharp
cache.Clear();
```

## Cache Key Format

Cache keys follow a consistent pattern: `{operation}:{schema}:{object}:{params}`

Examples:
- `tables:dbo` - Tables in dbo schema
- `tablecolumns:dbo:Users` - Columns for dbo.Users table
- `tablesampledata:dbo:Orders:10:True` - Sample data (10 records, random)

## Performance Considerations

### Cache Hit Scenarios
- Repeated queries for the same schema information
- Multiple requests for table/view metadata
- Frequent sample data requests

### Cache Miss Scenarios
- First-time data requests
- Data requests after TTL expiration
- Requests after cache invalidation

### Best Practices

1. **Schema Changes**: Invalidate relevant cache patterns when schema changes occur
2. **Memory Usage**: Monitor cache size and adjust TTL settings based on available memory
3. **TTL Tuning**: Adjust TTL values based on how frequently your schema changes
4. **Sample Data**: Use shorter TTL for sample data as it may change more frequently

## Monitoring and Troubleshooting

### Enable Debug Logging

The cache implementation includes structured logging. Enable debug level logging to see cache operations:

```csharp
services.AddLogging(builder =>
{
    builder.AddDebug()
           .SetMinimumLevel(LogLevel.Debug);
});
```

### Cache Metrics

Monitor these key metrics:
- **Hit Ratio**: Should be > 70% for good performance
- **Cache Size**: Monitor to avoid memory pressure
- **Miss Count**: High misses may indicate TTL is too short

### Common Issues

1. **Low Hit Ratio**: Increase TTL values or check if cache is being cleared too frequently
2. **Memory Issues**: Reduce TTL values or implement cache size limits
3. **Stale Data**: Reduce TTL values or implement proper cache invalidation

## Future Enhancements

The current implementation provides a foundation for:

1. **Distributed Caching**: Replace `InMemorySemanticModelCache` with Redis implementation
2. **Cache Warming**: Pre-populate frequently accessed data
3. **Advanced Invalidation**: Schema change detection and automatic invalidation
4. **Tiered Caching**: Combine in-memory and distributed caching strategies

## Thread Safety

The cache implementation is fully thread-safe and can be used in concurrent scenarios without additional synchronization.