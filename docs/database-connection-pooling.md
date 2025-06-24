# Database Connection Pooling Configuration

This document describes the database connection pooling functionality implemented in the GenAI Database Explorer.

## Overview

The application now supports configurable database connection pooling to improve performance and scalability when handling multiple concurrent operations. Instead of maintaining a single long-lived connection, the system uses SQL Server's built-in connection pooling with configurable parameters.

## Configuration Options

All connection pooling settings are configured in the `Database` section of your project settings:

### Pool Size Settings
- **`MaxPoolSize`** (default: 100): Maximum number of connections allowed in the pool
- **`MinPoolSize`** (default: 5): Minimum number of connections maintained in the pool
- **`PoolingEnabled`** (default: true): Whether connection pooling is enabled

### Timeout Settings
- **`ConnectionTimeout`** (default: 30): Connection timeout in seconds
- **`CommandTimeout`** (default: 30): Command timeout in seconds

### Retry and Resilience
- **`MaxRetryAttempts`** (default: 3): Maximum number of connection retry attempts
- **`RetryDelayMilliseconds`** (default: 1000): Delay between retry attempts in milliseconds
- **`EnableHealthMonitoring`** (default: true): Whether to perform connection health checks

## Example Configuration

```json
{
  "Database": {
    "Name": "MyDatabase",
    "ConnectionString": "Server=localhost;Database=MyDB;Integrated Security=true;",
    "MaxPoolSize": 100,
    "MinPoolSize": 5,
    "ConnectionTimeout": 30,
    "CommandTimeout": 30,
    "PoolingEnabled": true,
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 1000,
    "EnableHealthMonitoring": true
  }
}
```

## Connection String Parameters

The system automatically adds the following parameters to your connection string when pooling is enabled:

- `Pooling=true/false` - Based on `PoolingEnabled` setting
- `Max Pool Size` - Based on `MaxPoolSize` setting
- `Min Pool Size` - Based on `MinPoolSize` setting
- `Connection Timeout` - Based on `ConnectionTimeout` setting
- `Command Timeout` - Based on `CommandTimeout` setting
- `Connection Reset=true` - Ensures clean connections from pool
- `Load Balance Timeout=30` - Helps with pool management

## Features

### Connection Pooling
- Leverages SQL Server's built-in connection pooling
- Configurable pool size limits
- Automatic connection lifecycle management

### Retry Logic
- Automatic retry for transient connection failures
- Configurable retry attempts and delays
- Intelligent error classification (transient vs. permanent)

### Health Monitoring
- Basic connection health checks using `SELECT 1`
- Configurable health monitoring enable/disable
- Logging of connection health status

### Metrics and Monitoring
- Connection usage metrics tracking
- Structured logging for troubleshooting
- Connection pool statistics

## Validation Rules

The system validates configuration settings:

- Pool sizes must be non-negative and `MinPoolSize <= MaxPoolSize`
- Timeout values must be positive
- Retry settings must be non-negative
- Warnings for potentially problematic configurations (very high pool sizes, etc.)
- Recommendation for `MultipleActiveResultSets=True` when using parallel operations

## Performance Considerations

### Recommended Settings
- **Production**: `MaxPoolSize=100`, `MinPoolSize=5`
- **Development**: `MaxPoolSize=50`, `MinPoolSize=2`
- **High-load scenarios**: Consider increasing `MaxPoolSize` up to 200-300

### Parallel Operations
When using `MaxDegreeOfParallelism > 1`, ensure your connection string includes:
```
MultipleActiveResultSets=True
```

### Connection Lifecycle
- Connections are obtained from the pool for each operation
- Connections are automatically returned to the pool when disposed
- No long-lived connections are maintained by the application

## Troubleshooting

### Common Issues
1. **Pool exhaustion**: Increase `MaxPoolSize` or check for connection leaks
2. **Slow startup**: Decrease `MinPoolSize` for faster application startup
3. **Timeout errors**: Increase `ConnectionTimeout` or `CommandTimeout`
4. **Transient failures**: Ensure `MaxRetryAttempts` and `RetryDelayMilliseconds` are appropriate

### Logging
Enable detailed logging to monitor:
- Connection pool usage statistics
- Retry attempts and failures
- Health check results
- Connection lifecycle events

## Migration from Single Connection

The previous version maintained a single long-lived connection. This new implementation:

1. **Improves concurrency**: Multiple operations can use separate connections
2. **Enhances reliability**: Automatic retry and health monitoring
3. **Provides better scalability**: Configurable pool sizes
4. **Maintains compatibility**: Existing code continues to work unchanged

No code changes are required in consuming applications - the `IDatabaseConnectionManager.GetOpenConnectionAsync()` interface remains the same.