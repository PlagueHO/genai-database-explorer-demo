using GenAIDBExplorer.Core.Models.Database;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents metadata for a semantic model without fully loaded entities.
/// </summary>
public interface ISemanticModelMetadata
{
    /// <summary>
    /// Gets the name of the semantic model.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    string Source { get; }
    
    /// <summary>
    /// Gets the description of the semantic model.
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// Gets the count of tables in the semantic model.
    /// </summary>
    int TableCount { get; }
    
    /// <summary>
    /// Gets the count of views in the semantic model.
    /// </summary>
    int ViewCount { get; }
    
    /// <summary>
    /// Gets the count of stored procedures in the semantic model.
    /// </summary>
    int StoredProcedureCount { get; }
    
    /// <summary>
    /// Gets the table identifiers (schema.name) without loading full entities.
    /// </summary>
    IEnumerable<string> TableIdentifiers { get; }
    
    /// <summary>
    /// Gets the view identifiers (schema.name) without loading full entities.
    /// </summary>
    IEnumerable<string> ViewIdentifiers { get; }
    
    /// <summary>
    /// Gets the stored procedure identifiers (schema.name) without loading full entities.
    /// </summary>
    IEnumerable<string> StoredProcedureIdentifiers { get; }
}