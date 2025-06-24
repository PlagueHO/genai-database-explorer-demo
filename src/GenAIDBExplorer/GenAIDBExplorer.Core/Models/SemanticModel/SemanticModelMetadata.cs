using System.Text.Json;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents metadata for a semantic model without fully loaded entities.
/// </summary>
public sealed class SemanticModelMetadata : ISemanticModelMetadata
{
    private readonly List<string> _tableIdentifiers;
    private readonly List<string> _viewIdentifiers;
    private readonly List<string> _storedProcedureIdentifiers;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticModelMetadata"/> class.
    /// </summary>
    /// <param name="name">The name of the semantic model.</param>
    /// <param name="source">The source of the semantic model.</param>
    /// <param name="description">The description of the semantic model.</param>
    /// <param name="tableIdentifiers">The table identifiers.</param>
    /// <param name="viewIdentifiers">The view identifiers.</param>
    /// <param name="storedProcedureIdentifiers">The stored procedure identifiers.</param>
    public SemanticModelMetadata(
        string name,
        string source,
        string? description,
        IEnumerable<string> tableIdentifiers,
        IEnumerable<string> viewIdentifiers,
        IEnumerable<string> storedProcedureIdentifiers)
    {
        Name = name;
        Source = source;
        Description = description;
        _tableIdentifiers = tableIdentifiers.ToList();
        _viewIdentifiers = viewIdentifiers.ToList();
        _storedProcedureIdentifiers = storedProcedureIdentifiers.ToList();
    }

    /// <summary>
    /// Gets the name of the semantic model.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the description of the semantic model.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the count of tables in the semantic model.
    /// </summary>
    public int TableCount => _tableIdentifiers.Count;

    /// <summary>
    /// Gets the count of views in the semantic model.
    /// </summary>
    public int ViewCount => _viewIdentifiers.Count;

    /// <summary>
    /// Gets the count of stored procedures in the semantic model.
    /// </summary>
    public int StoredProcedureCount => _storedProcedureIdentifiers.Count;

    /// <summary>
    /// Gets the table identifiers (schema.name) without loading full entities.
    /// </summary>
    public IEnumerable<string> TableIdentifiers => _tableIdentifiers.AsReadOnly();

    /// <summary>
    /// Gets the view identifiers (schema.name) without loading full entities.
    /// </summary>
    public IEnumerable<string> ViewIdentifiers => _viewIdentifiers.AsReadOnly();

    /// <summary>
    /// Gets the stored procedure identifiers (schema.name) without loading full entities.
    /// </summary>
    public IEnumerable<string> StoredProcedureIdentifiers => _storedProcedureIdentifiers.AsReadOnly();

    /// <summary>
    /// Creates a <see cref="SemanticModelMetadata"/> from a model path.
    /// </summary>
    /// <param name="modelPath">The model path.</param>
    /// <returns>The semantic model metadata.</returns>
    public static async Task<SemanticModelMetadata> LoadFromPathAsync(DirectoryInfo modelPath)
    {
        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        if (!File.Exists(semanticModelJsonPath))
        {
            throw new FileNotFoundException("The semantic model file was not found.", semanticModelJsonPath);
        }

        await using var stream = File.OpenRead(semanticModelJsonPath);
        var jsonDocument = await JsonDocument.ParseAsync(stream);

        var name = jsonDocument.RootElement.GetProperty("Name").GetString() ?? string.Empty;
        var source = jsonDocument.RootElement.GetProperty("Source").GetString() ?? string.Empty;
        var description = jsonDocument.RootElement.TryGetProperty("Description", out var descElement) 
            ? descElement.GetString() 
            : null;

        var tableIdentifiers = new List<string>();
        if (jsonDocument.RootElement.TryGetProperty("Tables", out var tablesElement))
        {
            foreach (var tableElement in tablesElement.EnumerateArray())
            {
                var schema = tableElement.GetProperty("Schema").GetString() ?? string.Empty;
                var tableName = tableElement.GetProperty("Name").GetString() ?? string.Empty;
                tableIdentifiers.Add($"{schema}.{tableName}");
            }
        }

        var viewIdentifiers = new List<string>();
        if (jsonDocument.RootElement.TryGetProperty("Views", out var viewsElement))
        {
            foreach (var viewElement in viewsElement.EnumerateArray())
            {
                var schema = viewElement.GetProperty("Schema").GetString() ?? string.Empty;
                var viewName = viewElement.GetProperty("Name").GetString() ?? string.Empty;
                viewIdentifiers.Add($"{schema}.{viewName}");
            }
        }

        var storedProcedureIdentifiers = new List<string>();
        if (jsonDocument.RootElement.TryGetProperty("StoredProcedures", out var storedProceduresElement))
        {
            foreach (var spElement in storedProceduresElement.EnumerateArray())
            {
                var schema = spElement.GetProperty("Schema").GetString() ?? string.Empty;
                var spName = spElement.GetProperty("Name").GetString() ?? string.Empty;
                storedProcedureIdentifiers.Add($"{schema}.{spName}");
            }
        }

        return new SemanticModelMetadata(
            name,
            source,
            description,
            tableIdentifiers,
            viewIdentifiers,
            storedProcedureIdentifiers);
    }
}