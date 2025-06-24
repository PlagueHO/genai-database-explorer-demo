using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.Services;

/// <summary>
/// Service for handling compression and decompression of semantic model files.
/// </summary>
public class CompressionService(ILogger<CompressionService> logger) : ICompressionService
{
    private const string UncompressedExtension = ".json";

    /// <summary>
    /// Writes compressed or uncompressed content to a file based on compression settings.
    /// </summary>
    /// <param name="filePath">The base file path (without extension).</param>
    /// <param name="content">The content to write.</param>
    /// <param name="compressionSettings">The compression settings to use.</param>
    /// <returns>A task that represents the asynchronous write operation and contains compression statistics.</returns>
    public async Task<CompressionResult> WriteFileAsync(string filePath, string content, CompressionSettings compressionSettings)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(compressionSettings);

        var stopwatch = Stopwatch.StartNew();
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var originalSize = contentBytes.Length;

        if (compressionSettings.Enabled)
        {
            var compressedFilePath = filePath + compressionSettings.FileExtension;
            
            logger.LogDebug("Writing compressed file to {FilePath}", compressedFilePath);
            
            await using var fileStream = File.Create(compressedFilePath);
            await using var gzipStream = new GZipStream(fileStream, compressionSettings.Level);
            await gzipStream.WriteAsync(contentBytes);

            stopwatch.Stop();
            var compressedSize = new FileInfo(compressedFilePath).Length;
            
            logger.LogInformation(
                "Compression completed for {FilePath}. Original: {OriginalSize} bytes, Compressed: {CompressedSize} bytes, Ratio: {Ratio}%",
                compressedFilePath, originalSize, compressedSize, 
                Math.Round((1.0 - (double)compressedSize / originalSize) * 100, 2));

            return new CompressionResult(compressedFilePath, originalSize, compressedSize, true, stopwatch.Elapsed);
        }
        else
        {
            var uncompressedFilePath = filePath + UncompressedExtension;
            
            logger.LogDebug("Writing uncompressed file to {FilePath}", uncompressedFilePath);

            await File.WriteAllTextAsync(uncompressedFilePath, content, Encoding.UTF8);

            stopwatch.Stop();
            var fileSize = new FileInfo(uncompressedFilePath).Length;
            
            logger.LogDebug("Uncompressed file written to {FilePath}, Size: {Size} bytes", uncompressedFilePath, fileSize);

            return new CompressionResult(uncompressedFilePath, originalSize, fileSize, false, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Reads and decompresses content from a file, automatically detecting compression.
    /// </summary>
    /// <param name="basePath">The base file path (without extension).</param>
    /// <returns>A task that represents the asynchronous read operation and contains the file content.</returns>
    public async Task<string> ReadFileAsync(string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        var actualFilePath = GetActualFilePath(basePath);
        if (actualFilePath == null)
        {
            throw new FileNotFoundException($"No semantic model file found at base path: {basePath}");
        }

        logger.LogDebug("Reading file from {FilePath}", actualFilePath);

        // Check if the file is compressed based on extension
        if (actualFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Decompressing file {FilePath}", actualFilePath);

            await using var fileStream = File.OpenRead(actualFilePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            logger.LogDebug("Successfully decompressed file {FilePath}", actualFilePath);
            return content;
        }
        else
        {
            logger.LogDebug("Reading uncompressed file {FilePath}", actualFilePath);
            return await File.ReadAllTextAsync(actualFilePath, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Checks if a file exists in either compressed or uncompressed format.
    /// </summary>
    /// <param name="basePath">The base file path (without extension).</param>
    /// <returns>True if the file exists in any format; otherwise, false.</returns>
    public bool FileExists(string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        return GetActualFilePath(basePath) != null;
    }

    /// <summary>
    /// Gets the actual file path, prioritizing compressed files if they exist.
    /// </summary>
    /// <param name="basePath">The base file path (without extension).</param>
    /// <returns>The actual file path or null if no file exists.</returns>
    public string? GetActualFilePath(string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);

        // Check for compressed files first (priority order)
        var compressedExtensions = new[] { ".json.gz", ".gz" };
        foreach (var extension in compressedExtensions)
        {
            var compressedPath = basePath + extension;
            if (File.Exists(compressedPath))
            {
                return compressedPath;
            }
        }

        // Check for uncompressed file
        var uncompressedPath = basePath + UncompressedExtension;
        if (File.Exists(uncompressedPath))
        {
            return uncompressedPath;
        }

        return null;
    }
}