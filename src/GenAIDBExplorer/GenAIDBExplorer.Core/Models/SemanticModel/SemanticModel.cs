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
    /// Saves the semantic model to a stream using progressive serialization.
    /// This method processes entities one at a time to minimize memory usage.
    /// </summary>
    /// <param name="stream">The stream to write the semantic model to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task SaveStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();

        // Write basic properties
        writer.WriteString("Name", Name);
        writer.WriteString("Source", Source);
        if (!string.IsNullOrEmpty(Description))
        {
            writer.WriteString("Description", Description);
        }

        // Write tables array
        writer.WriteStartArray("Tables");
        foreach (var table in Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteTableAsync(writer, table);
        }
        writer.WriteEndArray();

        // Write views array
        writer.WriteStartArray("Views");
        foreach (var view in Views)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteViewAsync(writer, view);
        }
        writer.WriteEndArray();

        // Write stored procedures array
        writer.WriteStartArray("StoredProcedures");
        foreach (var storedProcedure in StoredProcedures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteStoredProcedureAsync(writer, storedProcedure);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a semantic model from a stream using progressive deserialization.
    /// This method processes entities as they are read to minimize memory usage.
    /// </summary>
    /// <param name="stream">The stream to read the semantic model from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous load operation that returns the loaded semantic model.</returns>
    public async Task<SemanticModel> LoadStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var name = root.GetProperty("Name").GetString() ?? throw new InvalidOperationException("Name property is required");
        var source = root.GetProperty("Source").GetString() ?? throw new InvalidOperationException("Source property is required");
        var description = root.TryGetProperty("Description", out var descriptionProperty) ? descriptionProperty.GetString() : null;

        var semanticModel = new SemanticModel(name, source, description);

        // Load tables progressively
        if (root.TryGetProperty("Tables", out var tablesProperty) && tablesProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var tableElement in tablesProperty.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var table = await ReadTableAsync(tableElement);
                semanticModel.AddTable(table);
            }
        }

        // Load views progressively  
        if (root.TryGetProperty("Views", out var viewsProperty) && viewsProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var viewElement in viewsProperty.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var view = await ReadViewAsync(viewElement);
                semanticModel.AddView(view);
            }
        }

        // Load stored procedures progressively
        if (root.TryGetProperty("StoredProcedures", out var storedProceduresProperty) && storedProceduresProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var spElement in storedProceduresProperty.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var storedProcedure = await ReadStoredProcedureAsync(spElement);
                semanticModel.AddStoredProcedure(storedProcedure);
            }
        }

        return semanticModel;
    }

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

        return semanticModel;
    }

    /// <summary>
    /// Gets the tables in the semantic model.
    /// </summary>
    public List<SemanticModelTable> Tables { get; set; } = [];

    /// <summary>
    /// Adds a table to the semantic model.
    /// </summary>
    /// <param name="table">The table to add.</param>
    public void AddTable(SemanticModelTable table)
    {
        Tables.Add(table);
    }

    /// <summary>
    /// Removes a table from the semantic model.
    /// </summary>
    /// <param name="table">The table to remove.</param>
    /// <returns>True if the table was removed; otherwise, false.</returns>
    public bool RemoveTable(SemanticModelTable table)
    {
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
        return Tables.FirstOrDefault(t => t.Schema == schemaName && t.Name == tableName);
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
            var matchingTable = Tables.FirstOrDefault(t => t.Schema == tableInfo.SchemaName && t.Name == tableInfo.TableName);
            if (matchingTable != null)
            {
                selectedTables.Add(matchingTable);
            }
        }

        return selectedTables;
    }

    /// <summary>
    /// Gets the views in the semantic model.
    /// </summary>
    public List<SemanticModelView> Views { get; set; } = [];

    /// <summary>
    /// Adds a view to the semantic model.
    /// </summary>
    /// <param name="view">The view to add.</param>
    public void AddView(SemanticModelView view)
    {
        Views.Add(view);
    }

    /// <summary>
    /// Removes a view from the semantic model.
    /// </summary>
    /// <param name="view">The view to remove.</param>
    /// <returns>True if the view was removed; otherwise, false.</returns>
    public bool RemoveView(SemanticModelView view)
    {
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
        return Views.FirstOrDefault(v => v.Schema == schemaName && v.Name == viewName);
    }

    /// <summary>
    /// Gets the stored procedures in the semantic model.
    /// </summary>
    public List<SemanticModelStoredProcedure> StoredProcedures { get; set; } = [];

    /// <summary>
    /// Adds a stored procedure to the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to add.</param>
    public void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        StoredProcedures.Add(storedProcedure);
    }

    /// <summary>
    /// Removes a stored procedure from the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to remove.</param>
    /// <returns>True if the stored procedure was removed; otherwise, false.</returns>
    public bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
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
        return StoredProcedures.FirstOrDefault(sp => sp.Schema == schemaName && sp.Name == storedProcedureName);
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

    #region Streaming Helper Methods

    /// <summary>
    /// Writes a table to the JSON writer.
    /// </summary>
    private static async Task WriteTableAsync(Utf8JsonWriter writer, SemanticModelTable table)
    {
        writer.WriteStartObject();
        writer.WriteString("Schema", table.Schema);
        writer.WriteString("Name", table.Name);
        if (!string.IsNullOrEmpty(table.Description))
            writer.WriteString("Description", table.Description);
        if (!string.IsNullOrEmpty(table.SemanticDescription))
            writer.WriteString("SemanticDescription", table.SemanticDescription);
        writer.WriteBoolean("NotUsed", table.NotUsed);
        if (!string.IsNullOrEmpty(table.NotUsedReason))
            writer.WriteString("NotUsedReason", table.NotUsedReason);

        // Write columns
        writer.WriteStartArray("Columns");
        foreach (var column in table.Columns)
        {
            writer.WriteStartObject();
            writer.WriteString("Schema", column.Schema);
            writer.WriteString("Name", column.Name);
            if (!string.IsNullOrEmpty(column.Description))
                writer.WriteString("Description", column.Description);
            if (!string.IsNullOrEmpty(column.SemanticDescription))
                writer.WriteString("SemanticDescription", column.SemanticDescription);
            writer.WriteBoolean("NotUsed", column.NotUsed);
            if (!string.IsNullOrEmpty(column.NotUsedReason))
                writer.WriteString("NotUsedReason", column.NotUsedReason);
            if (!string.IsNullOrEmpty(column.Type))
                writer.WriteString("Type", column.Type);
            writer.WriteBoolean("IsNullable", column.IsNullable);
            writer.WriteBoolean("IsPrimaryKey", column.IsPrimaryKey);
            writer.WriteBoolean("IsForeignKey", column.IsForeignKey);
            if (!string.IsNullOrEmpty(column.ForeignKeyTable))
                writer.WriteString("ForeignKeyTable", column.ForeignKeyTable);
            if (!string.IsNullOrEmpty(column.ForeignKeyColumn))
                writer.WriteString("ForeignKeyColumn", column.ForeignKeyColumn);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // Write indexes
        writer.WriteStartArray("Indexes");
        foreach (var index in table.Indexes)
        {
            writer.WriteStartObject();
            writer.WriteString("Schema", index.Schema);
            writer.WriteString("Name", index.Name);
            if (!string.IsNullOrEmpty(index.Description))
                writer.WriteString("Description", index.Description);
            if (!string.IsNullOrEmpty(index.SemanticDescription))
                writer.WriteString("SemanticDescription", index.SemanticDescription);
            writer.WriteBoolean("NotUsed", index.NotUsed);
            if (!string.IsNullOrEmpty(index.NotUsedReason))
                writer.WriteString("NotUsedReason", index.NotUsedReason);
            writer.WriteBoolean("IsUnique", index.IsUnique);
            writer.WriteBoolean("IsPrimaryKey", index.IsPrimaryKey);
            writer.WriteString("Columns", index.Columns);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Writes a view to the JSON writer.
    /// </summary>
    private static async Task WriteViewAsync(Utf8JsonWriter writer, SemanticModelView view)
    {
        writer.WriteStartObject();
        writer.WriteString("Schema", view.Schema);
        writer.WriteString("Name", view.Name);
        if (!string.IsNullOrEmpty(view.Description))
            writer.WriteString("Description", view.Description);
        if (!string.IsNullOrEmpty(view.SemanticDescription))
            writer.WriteString("SemanticDescription", view.SemanticDescription);
        writer.WriteBoolean("NotUsed", view.NotUsed);
        if (!string.IsNullOrEmpty(view.NotUsedReason))
            writer.WriteString("NotUsedReason", view.NotUsedReason);
        if (!string.IsNullOrEmpty(view.Definition))
            writer.WriteString("Definition", view.Definition);

        // Write columns
        writer.WriteStartArray("Columns");
        foreach (var column in view.Columns)
        {
            writer.WriteStartObject();
            writer.WriteString("Schema", column.Schema);
            writer.WriteString("Name", column.Name);
            if (!string.IsNullOrEmpty(column.Description))
                writer.WriteString("Description", column.Description);
            if (!string.IsNullOrEmpty(column.SemanticDescription))
                writer.WriteString("SemanticDescription", column.SemanticDescription);
            writer.WriteBoolean("NotUsed", column.NotUsed);
            if (!string.IsNullOrEmpty(column.NotUsedReason))
                writer.WriteString("NotUsedReason", column.NotUsedReason);
            if (!string.IsNullOrEmpty(column.Type))
                writer.WriteString("Type", column.Type);
            writer.WriteBoolean("IsNullable", column.IsNullable);
            writer.WriteBoolean("IsPrimaryKey", column.IsPrimaryKey);
            writer.WriteBoolean("IsForeignKey", column.IsForeignKey);
            if (!string.IsNullOrEmpty(column.ForeignKeyTable))
                writer.WriteString("ForeignKeyTable", column.ForeignKeyTable);
            if (!string.IsNullOrEmpty(column.ForeignKeyColumn))
                writer.WriteString("ForeignKeyColumn", column.ForeignKeyColumn);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Writes a stored procedure to the JSON writer.
    /// </summary>
    private static async Task WriteStoredProcedureAsync(Utf8JsonWriter writer, SemanticModelStoredProcedure storedProcedure)
    {
        writer.WriteStartObject();
        writer.WriteString("Schema", storedProcedure.Schema);
        writer.WriteString("Name", storedProcedure.Name);
        if (!string.IsNullOrEmpty(storedProcedure.Description))
            writer.WriteString("Description", storedProcedure.Description);
        if (!string.IsNullOrEmpty(storedProcedure.SemanticDescription))
            writer.WriteString("SemanticDescription", storedProcedure.SemanticDescription);
        writer.WriteBoolean("NotUsed", storedProcedure.NotUsed);
        if (!string.IsNullOrEmpty(storedProcedure.NotUsedReason))
            writer.WriteString("NotUsedReason", storedProcedure.NotUsedReason);
        writer.WriteString("Definition", storedProcedure.Definition);
        if (!string.IsNullOrEmpty(storedProcedure.Parameters))
            writer.WriteString("Parameters", storedProcedure.Parameters);
        writer.WriteEndObject();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Reads a table from a JSON element.
    /// </summary>
    private static async Task<SemanticModelTable> ReadTableAsync(JsonElement tableElement)
    {
        var schema = tableElement.GetProperty("Schema").GetString() ?? throw new InvalidOperationException("Schema is required for table");
        var name = tableElement.GetProperty("Name").GetString() ?? throw new InvalidOperationException("Name is required for table");
        var description = tableElement.TryGetProperty("Description", out var descProperty) ? descProperty.GetString() : null;

        var table = new SemanticModelTable(schema, name, description);

        if (tableElement.TryGetProperty("SemanticDescription", out var semanticDescProperty))
            table.SemanticDescription = semanticDescProperty.GetString();
        if (tableElement.TryGetProperty("NotUsed", out var notUsedProperty))
            table.NotUsed = notUsedProperty.GetBoolean();
        if (tableElement.TryGetProperty("NotUsedReason", out var notUsedReasonProperty))
            table.NotUsedReason = notUsedReasonProperty.GetString();

        // Read columns
        if (tableElement.TryGetProperty("Columns", out var columnsProperty) && columnsProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var columnElement in columnsProperty.EnumerateArray())
            {
                var column = await ReadColumnAsync(columnElement);
                table.AddColumn(column);
            }
        }

        // Read indexes
        if (tableElement.TryGetProperty("Indexes", out var indexesProperty) && indexesProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var indexElement in indexesProperty.EnumerateArray())
            {
                var index = await ReadIndexAsync(indexElement);
                table.AddIndex(index);
            }
        }

        return table;
    }

    /// <summary>
    /// Reads a view from a JSON element.
    /// </summary>
    private static async Task<SemanticModelView> ReadViewAsync(JsonElement viewElement)
    {
        var schema = viewElement.GetProperty("Schema").GetString() ?? throw new InvalidOperationException("Schema is required for view");
        var name = viewElement.GetProperty("Name").GetString() ?? throw new InvalidOperationException("Name is required for view");
        var description = viewElement.TryGetProperty("Description", out var descProperty) ? descProperty.GetString() : null;

        var view = new SemanticModelView(schema, name, description);

        if (viewElement.TryGetProperty("SemanticDescription", out var semanticDescProperty))
            view.SemanticDescription = semanticDescProperty.GetString();
        if (viewElement.TryGetProperty("NotUsed", out var notUsedProperty))
            view.NotUsed = notUsedProperty.GetBoolean();
        if (viewElement.TryGetProperty("NotUsedReason", out var notUsedReasonProperty))
            view.NotUsedReason = notUsedReasonProperty.GetString();
        if (viewElement.TryGetProperty("Definition", out var definitionProperty))
            view.Definition = definitionProperty.GetString();

        // Read columns
        if (viewElement.TryGetProperty("Columns", out var columnsProperty) && columnsProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var columnElement in columnsProperty.EnumerateArray())
            {
                var column = await ReadColumnAsync(columnElement);
                view.AddColumn(column);
            }
        }

        return view;
    }

    /// <summary>
    /// Reads a stored procedure from a JSON element.
    /// </summary>
    private static async Task<SemanticModelStoredProcedure> ReadStoredProcedureAsync(JsonElement spElement)
    {
        var schema = spElement.GetProperty("Schema").GetString() ?? throw new InvalidOperationException("Schema is required for stored procedure");
        var name = spElement.GetProperty("Name").GetString() ?? throw new InvalidOperationException("Name is required for stored procedure");
        var definition = spElement.GetProperty("Definition").GetString() ?? throw new InvalidOperationException("Definition is required for stored procedure");
        var parameters = spElement.TryGetProperty("Parameters", out var parametersProperty) ? parametersProperty.GetString() : null;
        var description = spElement.TryGetProperty("Description", out var descProperty) ? descProperty.GetString() : null;

        var storedProcedure = new SemanticModelStoredProcedure(schema, name, definition, parameters, description);

        if (spElement.TryGetProperty("SemanticDescription", out var semanticDescProperty))
            storedProcedure.SemanticDescription = semanticDescProperty.GetString();
        if (spElement.TryGetProperty("NotUsed", out var notUsedProperty))
            storedProcedure.NotUsed = notUsedProperty.GetBoolean();
        if (spElement.TryGetProperty("NotUsedReason", out var notUsedReasonProperty))
            storedProcedure.NotUsedReason = notUsedReasonProperty.GetString();

        return await Task.FromResult(storedProcedure);
    }

    /// <summary>
    /// Reads a column from a JSON element.
    /// </summary>
    private static async Task<SemanticModelColumn> ReadColumnAsync(JsonElement columnElement)
    {
        var schema = columnElement.GetProperty("Schema").GetString() ?? throw new InvalidOperationException("Schema is required for column");
        var name = columnElement.GetProperty("Name").GetString() ?? throw new InvalidOperationException("Name is required for column");
        var description = columnElement.TryGetProperty("Description", out var descProperty) ? descProperty.GetString() : null;

        var column = new SemanticModelColumn(schema, name, description);

        if (columnElement.TryGetProperty("SemanticDescription", out var semanticDescProperty))
            column.SemanticDescription = semanticDescProperty.GetString();
        if (columnElement.TryGetProperty("NotUsed", out var notUsedProperty))
            column.NotUsed = notUsedProperty.GetBoolean();
        if (columnElement.TryGetProperty("NotUsedReason", out var notUsedReasonProperty))
            column.NotUsedReason = notUsedReasonProperty.GetString();
        if (columnElement.TryGetProperty("Type", out var typeProperty))
            column.Type = typeProperty.GetString();
        if (columnElement.TryGetProperty("IsNullable", out var isNullableProperty))
            column.IsNullable = isNullableProperty.GetBoolean();
        if (columnElement.TryGetProperty("IsPrimaryKey", out var isPrimaryKeyProperty))
            column.IsPrimaryKey = isPrimaryKeyProperty.GetBoolean();
        if (columnElement.TryGetProperty("IsForeignKey", out var isForeignKeyProperty))
            column.IsForeignKey = isForeignKeyProperty.GetBoolean();
        if (columnElement.TryGetProperty("ForeignKeyTable", out var foreignKeyTableProperty))
            column.ForeignKeyTable = foreignKeyTableProperty.GetString();
        if (columnElement.TryGetProperty("ForeignKeyColumn", out var foreignKeyColumnProperty))
            column.ForeignKeyColumn = foreignKeyColumnProperty.GetString();

        return await Task.FromResult(column);
    }

    /// <summary>
    /// Reads an index from a JSON element.
    /// </summary>
    private static async Task<SemanticModelIndex> ReadIndexAsync(JsonElement indexElement)
    {
        var schema = indexElement.GetProperty("Schema").GetString() ?? throw new InvalidOperationException("Schema is required for index");
        var name = indexElement.GetProperty("Name").GetString() ?? throw new InvalidOperationException("Name is required for index");
        var columns = indexElement.GetProperty("Columns").GetString() ?? throw new InvalidOperationException("Columns is required for index");
        var description = indexElement.TryGetProperty("Description", out var descProperty) ? descProperty.GetString() : null;

        var index = new SemanticModelIndex(schema, name, columns, description);

        if (indexElement.TryGetProperty("SemanticDescription", out var semanticDescProperty))
            index.SemanticDescription = semanticDescProperty.GetString();
        if (indexElement.TryGetProperty("NotUsed", out var notUsedProperty))
            index.NotUsed = notUsedProperty.GetBoolean();
        if (indexElement.TryGetProperty("NotUsedReason", out var notUsedReasonProperty))
            index.NotUsedReason = notUsedReasonProperty.GetString();
        if (indexElement.TryGetProperty("IsUnique", out var isUniqueProperty))
            index.IsUnique = isUniqueProperty.GetBoolean();
        if (indexElement.TryGetProperty("IsPrimaryKey", out var isPrimaryKeyProperty))
            index.IsPrimaryKey = isPrimaryKeyProperty.GetBoolean();

        return await Task.FromResult(index);
    }

    #endregion

}