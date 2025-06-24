using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Defines the contract for streaming serialization and deserialization of semantic models.
/// Provides memory-efficient alternatives to directory-based persistence for large models.
/// </summary>
public interface IStreamableSemanticModel
{
    /// <summary>
    /// Saves the semantic model to a stream using progressive serialization.
    /// This method processes entities one at a time to minimize memory usage.
    /// </summary>
    /// <param name="stream">The stream to write the semantic model to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveStreamAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a semantic model from a stream using progressive deserialization.
    /// This method processes entities as they are read to minimize memory usage.
    /// </summary>
    /// <param name="stream">The stream to read the semantic model from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous load operation that returns the loaded semantic model.</returns>
    Task<SemanticModel> LoadStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}