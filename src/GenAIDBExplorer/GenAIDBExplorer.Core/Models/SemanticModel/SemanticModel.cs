using System.Text.Json;
using System.Text.Json.Serialization;
using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.SemanticModel.JsonConverters;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model for a database.
/// </summary>
public sealed class SemanticModel(
    string name,
    string source,
    string? description = null
    ) : ISemanticModel
{
    /// <summary>
    /// Gets the name of the semantic model.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    public string Source { get; set; } = source;

    /// <summary>
    /// Gets the description of the semantic model.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; } = description;

    /// <summary>
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model will be saved.</param>
    public async Task SaveModelAsync(DirectoryInfo modelPath)
    {
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        // Save the semantic model to a JSON file.
        Directory.CreateDirectory(modelPath.FullName);

        // Save the tables to separate files in a subfolder called "tables".
        var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
        Directory.CreateDirectory(tablesFolderPath.FullName);

        foreach (var table in Tables)
        {
            await table.SaveModelAsync(tablesFolderPath);
        }

        // Save the views to separate files in a subfolder called "views".
        var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
        Directory.CreateDirectory(viewsFolderPath.FullName);

        foreach (var view in Views)
        {
            await view.SaveModelAsync(viewsFolderPath);
        }

        // Save the stored procedures to separate files in a subfolder called "storedprocedures".
        var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
        Directory.CreateDirectory(storedProceduresFolderPath.FullName);

        foreach (var storedProcedure in StoredProcedures)
        {
            await storedProcedure.SaveModelAsync(storedProceduresFolderPath);
        }

        // Add custom converters for the tables, views, and stored procedures
        // to only serialize the name, schema and relative path of the entity.
        jsonSerializerOptions.Converters.Add(new SemanticModelTableJsonConverter());
        jsonSerializerOptions.Converters.Add(new SemanticModelViewJsonConverter());
        jsonSerializerOptions.Converters.Add(new SemanticModelStoredProcedureJsonConverter());

        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        await File.WriteAllTextAsync(semanticModelJsonPath, JsonSerializer.Serialize(this, jsonSerializerOptions));
    }

    /// <summary>
    /// Loads the semantic model from the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model is located.</param>
    /// <returns>The loaded semantic model.</returns>
    public async Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
    {
        JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        var semanticModelJsonPath = Path.Combine(modelPath.FullName, "semanticmodel.json");
        if (!File.Exists(semanticModelJsonPath))
        {
            throw new FileNotFoundException("The semantic model file was not found.", semanticModelJsonPath);
        }

        await using var stream = File.OpenRead(semanticModelJsonPath);
        var semanticModel = await JsonSerializer.DeserializeAsync<SemanticModel>(stream, jsonSerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize the semantic model.");

        // Load the tables listed in the model from the files in the "tables" subfolder.
        var tablesFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "tables"));
        if (Directory.Exists(tablesFolderPath.FullName))
        {
            foreach (var table in semanticModel.Tables)
            {
                await table.LoadModelAsync(tablesFolderPath);
            }
        }

        // Load the views listed in the model from the files in the "views" subfolder.
        var viewsFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "views"));
        if (Directory.Exists(viewsFolderPath.FullName))
        {
            foreach (var view in semanticModel.Views)
            {
                await view.LoadModelAsync(viewsFolderPath);
            }
        }

        // Load the stored procedures listed in the model from the files in the "storedprocedures" subfolder.
        var storedProceduresFolderPath = new DirectoryInfo(Path.Combine(modelPath.FullName, "storedprocedures"));
        if (Directory.Exists(storedProceduresFolderPath.FullName))
        {
            foreach (var storedProcedure in semanticModel.StoredProcedures)
            {
                await storedProcedure.LoadModelAsync(storedProceduresFolderPath);
            }
        }

        // Rebuild indexes after loading all data
        semanticModel.RebuildIndexes();

        return semanticModel;
    }

    /// <summary>
    /// Rebuilds all internal indexes after loading data from files or deserialization
    /// </summary>
    private void RebuildIndexes()
    {
        RebuildTablesIndex();
        RebuildViewsIndex();
        RebuildStoredProceduresIndex();
    }

    /// <summary>
    /// Dictionary for O(1) table lookups using composite key "schema.tablename"
    /// </summary>
    private readonly Dictionary<string, SemanticModelTable> _tableIndex = new();

    /// <summary>
    /// Backing field for Tables property
    /// </summary>
    private List<SemanticModelTable> _tables = [];

    /// <summary>
    /// Gets or sets the tables in the semantic model.
    /// </summary>
    public List<SemanticModelTable> Tables 
    { 
        get => _tables;
        set 
        {
            _tables = value;
            RebuildTablesIndex();
        }
    }

    /// <summary>
    /// Rebuilds the tables index
    /// </summary>
    private void RebuildTablesIndex()
    {
        _tableIndex.Clear();
        foreach (var table in _tables)
        {
            var key = GenerateCompositeKey(table.Schema, table.Name);
            _tableIndex[key] = table;
        }
    }

    /// <summary>
    /// Adds a table to the semantic model.
    /// </summary>
    /// <param name="table">The table to add.</param>
    public void AddTable(SemanticModelTable table)
    {
        Tables.Add(table);
        var key = GenerateCompositeKey(table.Schema, table.Name);
        _tableIndex[key] = table;
    }

    /// <summary>
    /// Removes a table from the semantic model.
    /// </summary>
    /// <param name="table">The table to remove.</param>
    /// <returns>True if the table was removed; otherwise, false.</returns>
    public bool RemoveTable(SemanticModelTable table)
    {
        var key = GenerateCompositeKey(table.Schema, table.Name);
        _tableIndex.Remove(key);
        return Tables.Remove(table);
    }

    /// <summary>
    /// Finds a table in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The table if found; otherwise, null.</returns>
    public SemanticModelTable? FindTable(string schemaName, string tableName)
    {
        var key = GenerateCompositeKey(schemaName, tableName);
        _tableIndex.TryGetValue(key, out var table);
        return table;
    }

    /// <summary>
    /// Selects tables from the semantic model that match the schema and table names in the provided TableList.
    /// </summary>
    /// <param name="tableList">The list of tables to match.</param>
    /// <returns>A list of matching SemanticModelTable objects.</returns>
    public List<SemanticModelTable> SelectTables(TableList tableList)
    {
        var selectedTables = new List<SemanticModelTable>();

        foreach (var tableInfo in tableList.Tables)
        {
            var key = GenerateCompositeKey(tableInfo.SchemaName, tableInfo.TableName);
            if (_tableIndex.TryGetValue(key, out var matchingTable))
            {
                selectedTables.Add(matchingTable);
            }
        }

        return selectedTables;
    }

    /// <summary>
    /// Dictionary for O(1) view lookups using composite key "schema.viewname"
    /// </summary>
    private readonly Dictionary<string, SemanticModelView> _viewIndex = new();

    /// <summary>
    /// Backing field for Views property
    /// </summary>
    private List<SemanticModelView> _views = [];

    /// <summary>
    /// Gets or sets the views in the semantic model.
    /// </summary>
    public List<SemanticModelView> Views 
    { 
        get => _views;
        set 
        {
            _views = value;
            RebuildViewsIndex();
        }
    }

    /// <summary>
    /// Rebuilds the views index
    /// </summary>
    private void RebuildViewsIndex()
    {
        _viewIndex.Clear();
        foreach (var view in _views)
        {
            var key = GenerateCompositeKey(view.Schema, view.Name);
            _viewIndex[key] = view;
        }
    }

    /// <summary>
    /// Adds a view to the semantic model.
    /// </summary>
    /// <param name="view">The view to add.</param>
    public void AddView(SemanticModelView view)
    {
        Views.Add(view);
        var key = GenerateCompositeKey(view.Schema, view.Name);
        _viewIndex[key] = view;
    }

    /// <summary>
    /// Removes a view from the semantic model.
    /// </summary>
    /// <param name="view">The view to remove.</param>
    /// <returns>True if the view was removed; otherwise, false.</returns>
    public bool RemoveView(SemanticModelView view)
    {
        var key = GenerateCompositeKey(view.Schema, view.Name);
        _viewIndex.Remove(key);
        return Views.Remove(view);
    }

    /// <summary>
    /// Finds a view in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The view if found; otherwise, null.</returns></returns>
    public SemanticModelView? FindView(string schemaName, string viewName)
    {
        var key = GenerateCompositeKey(schemaName, viewName);
        _viewIndex.TryGetValue(key, out var view);
        return view;
    }

    /// <summary>
    /// Dictionary for O(1) stored procedure lookups using composite key "schema.procedurename"
    /// </summary>
    private readonly Dictionary<string, SemanticModelStoredProcedure> _storedProcedureIndex = new();

    /// <summary>
    /// Backing field for StoredProcedures property
    /// </summary>
    private List<SemanticModelStoredProcedure> _storedProcedures = [];

    /// <summary>
    /// Gets or sets the stored procedures in the semantic model.
    /// </summary>
    public List<SemanticModelStoredProcedure> StoredProcedures 
    { 
        get => _storedProcedures;
        set 
        {
            _storedProcedures = value;
            RebuildStoredProceduresIndex();
        }
    }

    /// <summary>
    /// Rebuilds the stored procedures index
    /// </summary>
    private void RebuildStoredProceduresIndex()
    {
        _storedProcedureIndex.Clear();
        foreach (var storedProcedure in _storedProcedures)
        {
            var key = GenerateCompositeKey(storedProcedure.Schema, storedProcedure.Name);
            _storedProcedureIndex[key] = storedProcedure;
        }
    }

    /// <summary>
    /// Generates a composite key for indexing using schema and name
    /// </summary>
    /// <param name="schema">The schema name</param>
    /// <param name="name">The entity name</param>
    /// <returns>Composite key in format "schema.name"</returns>
    private static string GenerateCompositeKey(string schema, string name)
    {
        return $"{schema}.{name}";
    }

    /// <summary>
    /// Adds a stored procedure to the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to add.</param>
    public void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        StoredProcedures.Add(storedProcedure);
        var key = GenerateCompositeKey(storedProcedure.Schema, storedProcedure.Name);
        _storedProcedureIndex[key] = storedProcedure;
    }

    /// <summary>
    /// Removes a stored procedure from the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to remove.</param>
    /// <returns>True if the stored procedure was removed; otherwise, false.</returns>
    public bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        var key = GenerateCompositeKey(storedProcedure.Schema, storedProcedure.Name);
        _storedProcedureIndex.Remove(key);
        return StoredProcedures.Remove(storedProcedure);
    }

    /// <summary>
    /// Finds a stored procedure in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The stored procedure if found; otherwise, null.</returns>
    public SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string storedProcedureName)
    {
        var key = GenerateCompositeKey(schemaName, storedProcedureName);
        _storedProcedureIndex.TryGetValue(key, out var storedProcedure);
        return storedProcedure;
    }

    /// <summary>
    /// Accepts a visitor to traverse the semantic model.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model.</param>
    public void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitSemanticModel(this);
        foreach (var table in Tables)
        {
            table.Accept(visitor);
        }

        foreach (var view in Views)
        {
            view.Accept(visitor);
        }

        foreach (var storedProcedure in StoredProcedures)
        {
            storedProcedure.Accept(visitor);
        }
    }

}