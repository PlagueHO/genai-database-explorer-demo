using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class DatabaseSettings : IValidatableObject
{
    // The settings key that contains the Database settings
    public const string PropertyName = "Database";

    /// <summary>
    /// The friendly name of the database.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the purpose of the database.
    /// This is used to ground the AI in the context of the database.
    /// It is not required, but it will improve the AI's understanding of the database.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Connection string to the database
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The list of schemas to extract from the database. If not specified, all schemas will be queried.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// The maximum number of parallel queries to run against the database. Requires MultipleActiveResultSets=True in the connection string.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;

    /// <summary>
    /// A list of regular expressions to set the 'NotUsed' flag on tables in the database.
    /// </summary>
    public List<string> NotUsedTables { get; set; } = [];

    /// <summary>
    /// A list of regular expressions to exclude columns from the database. These will have 'NotUsed' set to true.
    /// </summary>
    public List<string> NotUsedColumns { get; set; } = [];

    /// <summary>
    /// A list of regular expressions to exclude views from the database. These will have 'NotUsed' set to true.
    /// </summary>
    public List<string> NotUsedViews { get; set; } = [];

    /// <summary>
    /// A list of regular expressions to exclude stored procedures from the database. These will have 'NotUsed' set to true.
    /// </summary>
    public List<string> NotUsedStoredProcedures { get; set; } = [];

    /// <summary>
    /// The maximum number of connections allowed in the connection pool. Default is 100.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// The minimum number of connections maintained in the connection pool. Default is 5.
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// The connection timeout in seconds. Default is 30 seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// The command timeout in seconds. Default is 30 seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Whether connection pooling is enabled. Default is true.
    /// </summary>
    public bool PoolingEnabled { get; set; } = true;

    /// <summary>
    /// The maximum number of connection retry attempts. Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// The delay between connection retry attempts in milliseconds. Default is 1000ms.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Whether to enable connection health monitoring. Default is true.
    /// </summary>
    public bool EnableHealthMonitoring { get; set; } = true;

    /// <summary>
    /// Validates the database settings configuration.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate pool size settings
        if (MinPoolSize < 0)
        {
            results.Add(new ValidationResult(
                "MinPoolSize must be non-negative.",
                new[] { nameof(MinPoolSize) }));
        }

        if (MaxPoolSize <= 0)
        {
            results.Add(new ValidationResult(
                "MaxPoolSize must be greater than zero.",
                new[] { nameof(MaxPoolSize) }));
        }

        if (MinPoolSize > MaxPoolSize)
        {
            results.Add(new ValidationResult(
                "MinPoolSize cannot be greater than MaxPoolSize.",
                new[] { nameof(MinPoolSize), nameof(MaxPoolSize) }));
        }

        // Validate timeout settings
        if (ConnectionTimeout <= 0)
        {
            results.Add(new ValidationResult(
                "ConnectionTimeout must be greater than zero.",
                new[] { nameof(ConnectionTimeout) }));
        }

        if (CommandTimeout <= 0)
        {
            results.Add(new ValidationResult(
                "CommandTimeout must be greater than zero.",
                new[] { nameof(CommandTimeout) }));
        }

        // Validate retry settings
        if (MaxRetryAttempts < 0)
        {
            results.Add(new ValidationResult(
                "MaxRetryAttempts must be non-negative.",
                new[] { nameof(MaxRetryAttempts) }));
        }

        if (RetryDelayMilliseconds < 0)
        {
            results.Add(new ValidationResult(
                "RetryDelayMilliseconds must be non-negative.",
                new[] { nameof(RetryDelayMilliseconds) }));
        }

        // Validate MaxDegreeOfParallelism
        if (MaxDegreeOfParallelism <= 0)
        {
            results.Add(new ValidationResult(
                "MaxDegreeOfParallelism must be greater than zero.",
                new[] { nameof(MaxDegreeOfParallelism) }));
        }

        // Warn about potentially problematic configurations
        if (MaxPoolSize > 1000)
        {
            results.Add(new ValidationResult(
                "MaxPoolSize is very high (>1000). Consider if this is necessary as it may consume excessive resources.",
                new[] { nameof(MaxPoolSize) }));
        }

        if (ConnectionTimeout > 300)
        {
            results.Add(new ValidationResult(
                "ConnectionTimeout is very high (>300 seconds). This may cause long delays in error scenarios.",
                new[] { nameof(ConnectionTimeout) }));
        }

        if (PoolingEnabled && MaxDegreeOfParallelism > 1 && !ConnectionString.Contains("MultipleActiveResultSets=True", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new ValidationResult(
                "When MaxDegreeOfParallelism > 1, the connection string should include 'MultipleActiveResultSets=True' for optimal performance.",
                new[] { nameof(MaxDegreeOfParallelism), nameof(ConnectionString) }));
        }

        return results;
    }
}
