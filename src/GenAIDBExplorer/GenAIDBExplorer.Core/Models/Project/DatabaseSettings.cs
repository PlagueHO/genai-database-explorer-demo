using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class DatabaseSettings
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
}
