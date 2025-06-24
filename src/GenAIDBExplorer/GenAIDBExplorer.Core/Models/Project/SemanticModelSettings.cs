using System.ComponentModel.DataAnnotations;

namespace GenAIDBExplorer.Core.Models.Project;

public class SemanticModelSettings
{
    // The settings key that contains the Database settings
    public const string PropertyName = "SemanticModel";
    
    /// <summary>
    /// The maximum number of parallel semantic model processes to run.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;

    /// <summary>
    /// Cache settings for semantic model operations.
    /// </summary>
    public SemanticModelCacheSettings Cache { get; set; } = new();
}

/// <summary>
/// Configuration settings for semantic model caching.
/// </summary>
public class SemanticModelCacheSettings
{
    /// <summary>
    /// Gets or sets whether caching is enabled. Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default TTL for cached tables in minutes. Default is 30 minutes.
    /// </summary>
    public int TablesTtlMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default TTL for cached views in minutes. Default is 30 minutes.
    /// </summary>
    public int ViewsTtlMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default TTL for cached stored procedures in minutes. Default is 30 minutes.
    /// </summary>
    public int StoredProceduresTtlMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default TTL for cached columns in minutes. Default is 60 minutes.
    /// </summary>
    public int ColumnsTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the default TTL for cached sample data in minutes. Default is 15 minutes.
    /// </summary>
    public int SampleDataTtlMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the default TTL for view definitions in minutes. Default is 60 minutes.
    /// </summary>
    public int ViewDefinitionsTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Gets the TTL for tables as a TimeSpan.
    /// </summary>
    public TimeSpan TablesTtl => TimeSpan.FromMinutes(TablesTtlMinutes);

    /// <summary>
    /// Gets the TTL for views as a TimeSpan.
    /// </summary>
    public TimeSpan ViewsTtl => TimeSpan.FromMinutes(ViewsTtlMinutes);

    /// <summary>
    /// Gets the TTL for stored procedures as a TimeSpan.
    /// </summary>
    public TimeSpan StoredProceduresTtl => TimeSpan.FromMinutes(StoredProceduresTtlMinutes);

    /// <summary>
    /// Gets the TTL for columns as a TimeSpan.
    /// </summary>
    public TimeSpan ColumnsTtl => TimeSpan.FromMinutes(ColumnsTtlMinutes);

    /// <summary>
    /// Gets the TTL for sample data as a TimeSpan.
    /// </summary>
    public TimeSpan SampleDataTtl => TimeSpan.FromMinutes(SampleDataTtlMinutes);

    /// <summary>
    /// Gets the TTL for view definitions as a TimeSpan.
    /// </summary>
    public TimeSpan ViewDefinitionsTtl => TimeSpan.FromMinutes(ViewDefinitionsTtlMinutes);
}
