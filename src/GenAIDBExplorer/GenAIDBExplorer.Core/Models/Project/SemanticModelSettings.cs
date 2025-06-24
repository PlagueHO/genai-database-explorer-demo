using System.ComponentModel.DataAnnotations;
using System.IO.Compression;

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
    /// Configuration settings for compression support.
    /// </summary>
    public CompressionSettings Compression { get; set; } = new();
}

/// <summary>
/// Configuration settings for semantic model file compression.
/// </summary>
public class CompressionSettings
{
    /// <summary>
    /// Enables or disables compression for semantic model files.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The compression level to use when compression is enabled.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;

    /// <summary>
    /// The file extension to use for compressed files.
    /// </summary>
    public string FileExtension { get; set; } = ".json.gz";
}
