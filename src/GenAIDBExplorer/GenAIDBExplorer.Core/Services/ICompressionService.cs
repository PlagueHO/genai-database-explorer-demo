using GenAIDBExplorer.Core.Models.Project;

namespace GenAIDBExplorer.Core.Services;

/// <summary>
/// Service for handling compression and decompression of semantic model files.
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Writes compressed or uncompressed content to a file based on compression settings.
    /// </summary>
    /// <param name="filePath">The base file path (without extension).</param>
    /// <param name="content">The content to write.</param>
    /// <param name="compressionSettings">The compression settings to use.</param>
    /// <returns>A task that represents the asynchronous write operation and contains compression statistics.</returns>
    Task<CompressionResult> WriteFileAsync(string filePath, string content, CompressionSettings compressionSettings);

    /// <summary>
    /// Reads and decompresses content from a file, automatically detecting compression.
    /// </summary>
    /// <param name="basePath">The base file path (without extension).</param>
    /// <returns>A task that represents the asynchronous read operation and contains the file content.</returns>
    Task<string> ReadFileAsync(string basePath);

    /// <summary>
    /// Checks if a file exists in either compressed or uncompressed format.
    /// </summary>
    /// <param name="basePath">The base file path (without extension).</param>
    /// <returns>True if the file exists in any format; otherwise, false.</returns>
    bool FileExists(string basePath);

    /// <summary>
    /// Gets the actual file path, prioritizing compressed files if they exist.
    /// </summary>
    /// <param name="basePath">The base file path (without extension).</param>
    /// <returns>The actual file path or null if no file exists.</returns>
    string? GetActualFilePath(string basePath);
}

/// <summary>
/// Results of a compression operation.
/// </summary>
public record CompressionResult(
    string FilePath,
    long OriginalSize,
    long CompressedSize,
    bool IsCompressed,
    TimeSpan CompressionTime)
{
    /// <summary>
    /// Gets the compression ratio as a percentage (0-100).
    /// </summary>
    public double CompressionRatio => IsCompressed && OriginalSize > 0 ? 
        Math.Round((1.0 - (double)CompressedSize / OriginalSize) * 100, 2) : 0;
}