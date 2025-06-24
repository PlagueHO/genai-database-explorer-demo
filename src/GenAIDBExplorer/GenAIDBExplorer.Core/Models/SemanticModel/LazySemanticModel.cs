using System.Collections.Concurrent;
using System.Text.Json;
using GenAIDBExplorer.Core.Models.Database;

namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model with lazy loading capabilities.
/// </summary>
public sealed class LazySemanticModel : ILazySemanticModel
{
    private readonly DirectoryInfo _modelPath;
    private readonly ConcurrentDictionary<string, SemanticModelTable> _loadedTables = new();
    private readonly ConcurrentDictionary<string, SemanticModelView> _loadedViews = new();
    private readonly ConcurrentDictionary<string, SemanticModelStoredProcedure> _loadedStoredProcedures = new();
    private readonly ISemanticModelMetadata _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazySemanticModel"/> class.
    /// </summary>
    /// <param name="metadata">The semantic model metadata.</param>
    /// <param name="modelPath">The model path.</param>
    public LazySemanticModel(ISemanticModelMetadata metadata, DirectoryInfo modelPath)
    {
        _metadata = metadata;
        _modelPath = modelPath;
        Name = metadata.Name;
        Source = metadata.Source;
        Description = metadata.Description;
    }

    /// <summary>
    /// Gets the name of the semantic model.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the source of the semantic model.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets the description of the semantic model.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the tables in the semantic model. This triggers lazy loading.
    /// </summary>
    public List<SemanticModelTable> Tables 
    { 
        get
        {
            // Load all tables if not already loaded
            var result = new List<SemanticModelTable>();
            foreach (var tableId in _metadata.TableIdentifiers)
            {
                var parts = tableId.Split('.');
                if (parts.Length == 2)
                {
                    var table = LoadTableAsync(parts[0], parts[1]).Result;
                    result.Add(table);
                }
            }
            return result;
        }
        set
        {
            // Clear existing cache and populate with new values
            _loadedTables.Clear();
            foreach (var table in value)
            {
                var key = $"{table.Schema}.{table.Name}";
                _loadedTables.TryAdd(key, table);
            }
        }
    }

    /// <summary>
    /// Gets the views in the semantic model. This triggers lazy loading.
    /// </summary>
    public List<SemanticModelView> Views 
    { 
        get
        {
            // Load all views if not already loaded
            var result = new List<SemanticModelView>();
            foreach (var viewId in _metadata.ViewIdentifiers)
            {
                var parts = viewId.Split('.');
                if (parts.Length == 2)
                {
                    var view = LoadViewAsync(parts[0], parts[1]).Result;
                    result.Add(view);
                }
            }
            return result;
        }
        set
        {
            // Clear existing cache and populate with new values
            _loadedViews.Clear();
            foreach (var view in value)
            {
                var key = $"{view.Schema}.{view.Name}";
                _loadedViews.TryAdd(key, view);
            }
        }
    }

    /// <summary>
    /// Gets the stored procedures in the semantic model. This triggers lazy loading.
    /// </summary>
    public List<SemanticModelStoredProcedure> StoredProcedures 
    { 
        get
        {
            // Load all stored procedures if not already loaded
            var result = new List<SemanticModelStoredProcedure>();
            foreach (var spId in _metadata.StoredProcedureIdentifiers)
            {
                var parts = spId.Split('.');
                if (parts.Length == 2)
                {
                    var sp = LoadStoredProcedureAsync(parts[0], parts[1]).Result;
                    result.Add(sp);
                }
            }
            return result;
        }
        set
        {
            // Clear existing cache and populate with new values
            _loadedStoredProcedures.Clear();
            foreach (var sp in value)
            {
                var key = $"{sp.Schema}.{sp.Name}";
                _loadedStoredProcedures.TryAdd(key, sp);
            }
        }
    }

    /// <summary>
    /// Loads a table on-demand by schema and table name.
    /// </summary>
    /// <param name="schema">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The loaded semantic model table.</returns>
    public async Task<SemanticModelTable> LoadTableAsync(string schema, string tableName)
    {
        var key = $"{schema}.{tableName}";
        
        if (_loadedTables.TryGetValue(key, out var cachedTable))
        {
            return cachedTable;
        }

        // Create a new table instance and load it from file
        var table = new SemanticModelTable(schema, tableName);
        var tablesFolderPath = new DirectoryInfo(Path.Combine(_modelPath.FullName, "tables"));
        
        if (Directory.Exists(tablesFolderPath.FullName))
        {
            await table.LoadModelAsync(tablesFolderPath);
        }

        _loadedTables.TryAdd(key, table);
        return table;
    }

    /// <summary>
    /// Loads a view on-demand by schema and view name.
    /// </summary>
    /// <param name="schema">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The loaded semantic model view.</returns>
    public async Task<SemanticModelView> LoadViewAsync(string schema, string viewName)
    {
        var key = $"{schema}.{viewName}";
        
        if (_loadedViews.TryGetValue(key, out var cachedView))
        {
            return cachedView;
        }

        // Create a new view instance and load it from file
        var view = new SemanticModelView(schema, viewName);
        var viewsFolderPath = new DirectoryInfo(Path.Combine(_modelPath.FullName, "views"));
        
        if (Directory.Exists(viewsFolderPath.FullName))
        {
            await view.LoadModelAsync(viewsFolderPath);
        }

        _loadedViews.TryAdd(key, view);
        return view;
    }

    /// <summary>
    /// Loads a stored procedure on-demand by schema and stored procedure name.
    /// </summary>
    /// <param name="schema">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The loaded semantic model stored procedure.</returns>
    public async Task<SemanticModelStoredProcedure> LoadStoredProcedureAsync(string schema, string storedProcedureName)
    {
        var key = $"{schema}.{storedProcedureName}";
        
        if (_loadedStoredProcedures.TryGetValue(key, out var cachedSp))
        {
            return cachedSp;
        }

        // Create a new stored procedure instance and load it from file
        var sp = new SemanticModelStoredProcedure(schema, storedProcedureName, string.Empty); // Will be populated by LoadModelAsync
        var spFolderPath = new DirectoryInfo(Path.Combine(_modelPath.FullName, "storedprocedures"));
        
        if (Directory.Exists(spFolderPath.FullName))
        {
            await sp.LoadModelAsync(spFolderPath);
        }

        _loadedStoredProcedures.TryAdd(key, sp);
        return sp;
    }

    /// <summary>
    /// Loads multiple tables on-demand by their identifiers.
    /// </summary>
    /// <param name="tableIds">The table identifiers in "schema.name" format.</param>
    /// <returns>The loaded semantic model tables.</returns>
    public async Task<List<SemanticModelTable>> LoadTablesAsync(IEnumerable<string> tableIds)
    {
        var tasks = tableIds.Select(async tableId =>
        {
            var parts = tableId.Split('.');
            if (parts.Length == 2)
            {
                return await LoadTableAsync(parts[0], parts[1]);
            }
            throw new ArgumentException($"Invalid table identifier format: {tableId}. Expected 'schema.name'.");
        });

        var tables = await Task.WhenAll(tasks);
        return tables.ToList();
    }

    /// <summary>
    /// Loads multiple views on-demand by their identifiers.
    /// </summary>
    /// <param name="viewIds">The view identifiers in "schema.name" format.</param>
    /// <returns>The loaded semantic model views.</returns>
    public async Task<List<SemanticModelView>> LoadViewsAsync(IEnumerable<string> viewIds)
    {
        var tasks = viewIds.Select(async viewId =>
        {
            var parts = viewId.Split('.');
            if (parts.Length == 2)
            {
                return await LoadViewAsync(parts[0], parts[1]);
            }
            throw new ArgumentException($"Invalid view identifier format: {viewId}. Expected 'schema.name'.");
        });

        var views = await Task.WhenAll(tasks);
        return views.ToList();
    }

    /// <summary>
    /// Loads multiple stored procedures on-demand by their identifiers.
    /// </summary>
    /// <param name="storedProcedureIds">The stored procedure identifiers in "schema.name" format.</param>
    /// <returns>The loaded semantic model stored procedures.</returns>
    public async Task<List<SemanticModelStoredProcedure>> LoadStoredProceduresAsync(IEnumerable<string> storedProcedureIds)
    {
        var tasks = storedProcedureIds.Select(async spId =>
        {
            var parts = spId.Split('.');
            if (parts.Length == 2)
            {
                return await LoadStoredProcedureAsync(parts[0], parts[1]);
            }
            throw new ArgumentException($"Invalid stored procedure identifier format: {spId}. Expected 'schema.name'.");
        });

        var sps = await Task.WhenAll(tasks);
        return sps.ToList();
    }

    /// <summary>
    /// Gets a value indicating whether a table is currently loaded in memory.
    /// </summary>
    /// <param name="schema">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>True if the table is loaded; otherwise, false.</returns>
    public bool IsTableLoaded(string schema, string tableName)
    {
        var key = $"{schema}.{tableName}";
        return _loadedTables.ContainsKey(key);
    }

    /// <summary>
    /// Gets a value indicating whether a view is currently loaded in memory.
    /// </summary>
    /// <param name="schema">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>True if the view is loaded; otherwise, false.</returns>
    public bool IsViewLoaded(string schema, string viewName)
    {
        var key = $"{schema}.{viewName}";
        return _loadedViews.ContainsKey(key);
    }

    /// <summary>
    /// Gets a value indicating whether a stored procedure is currently loaded in memory.
    /// </summary>
    /// <param name="schema">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>True if the stored procedure is loaded; otherwise, false.</returns>
    public bool IsStoredProcedureLoaded(string schema, string storedProcedureName)
    {
        var key = $"{schema}.{storedProcedureName}";
        return _loadedStoredProcedures.ContainsKey(key);
    }

    // ISemanticModel interface implementation

    /// <summary>
    /// Saves the semantic model to the specified folder.
    /// </summary>
    /// <param name="modelPath">The folder path where the model will be saved.</param>
    public async Task SaveModelAsync(DirectoryInfo modelPath)
    {
        // Create a regular SemanticModel with all loaded entities and delegate to it
        var regularModel = new SemanticModel(Name, Source, Description);
        
        // Add all loaded entities
        foreach (var table in _loadedTables.Values)
        {
            regularModel.AddTable(table);
        }
        
        foreach (var view in _loadedViews.Values)
        {
            regularModel.AddView(view);
        }
        
        foreach (var sp in _loadedStoredProcedures.Values)
        {
            regularModel.AddStoredProcedure(sp);
        }

        await regularModel.SaveModelAsync(modelPath);
    }

    /// <summary>
    /// This method is not supported for lazy semantic models. Use LoadModelLazyAsync instead.
    /// </summary>
    /// <param name="modelPath">The folder path where the model is located.</param>
    /// <returns>The loaded semantic model.</returns>
    public Task<SemanticModel> LoadModelAsync(DirectoryInfo modelPath)
    {
        throw new NotSupportedException("LoadModelAsync is not supported for lazy semantic models. Use LoadModelLazyAsync from ISemanticModelProvider instead.");
    }

    /// <summary>
    /// Adds a table to the semantic model.
    /// </summary>
    /// <param name="table">The table to add.</param>
    public void AddTable(SemanticModelTable table)
    {
        var key = $"{table.Schema}.{table.Name}";
        _loadedTables.AddOrUpdate(key, table, (k, v) => table);
    }

    /// <summary>
    /// Removes a table from the semantic model.
    /// </summary>
    /// <param name="table">The table to remove.</param>
    /// <returns>True if the table was removed; otherwise, false.</returns>
    public bool RemoveTable(SemanticModelTable table)
    {
        var key = $"{table.Schema}.{table.Name}";
        return _loadedTables.TryRemove(key, out _);
    }

    /// <summary>
    /// Finds a table in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The table if found; otherwise, null.</returns>
    public SemanticModelTable? FindTable(string schemaName, string tableName)
    {
        // First check if it's already loaded
        var key = $"{schemaName}.{tableName}";
        if (_loadedTables.TryGetValue(key, out var table))
        {
            return table;
        }

        // Check if it exists in metadata
        if (_metadata.TableIdentifiers.Contains(key))
        {
            // Load it on demand
            return LoadTableAsync(schemaName, tableName).Result;
        }

        return null;
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
            var matchingTable = FindTable(tableInfo.SchemaName, tableInfo.TableName);
            if (matchingTable != null)
            {
                selectedTables.Add(matchingTable);
            }
        }

        return selectedTables;
    }

    /// <summary>
    /// Adds a view to the semantic model.
    /// </summary>
    /// <param name="view">The view to add.</param>
    public void AddView(SemanticModelView view)
    {
        var key = $"{view.Schema}.{view.Name}";
        _loadedViews.AddOrUpdate(key, view, (k, v) => view);
    }

    /// <summary>
    /// Removes a view from the semantic model.
    /// </summary>
    /// <param name="view">The view to remove.</param>
    /// <returns>True if the view was removed; otherwise, false.</returns>
    public bool RemoveView(SemanticModelView view)
    {
        var key = $"{view.Schema}.{view.Name}";
        return _loadedViews.TryRemove(key, out _);
    }

    /// <summary>
    /// Finds a view in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The view if found; otherwise, null.</returns>
    public SemanticModelView? FindView(string schemaName, string viewName)
    {
        // First check if it's already loaded
        var key = $"{schemaName}.{viewName}";
        if (_loadedViews.TryGetValue(key, out var view))
        {
            return view;
        }

        // Check if it exists in metadata
        if (_metadata.ViewIdentifiers.Contains(key))
        {
            // Load it on demand
            return LoadViewAsync(schemaName, viewName).Result;
        }

        return null;
    }

    /// <summary>
    /// Adds a stored procedure to the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to add.</param>
    public void AddStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        var key = $"{storedProcedure.Schema}.{storedProcedure.Name}";
        _loadedStoredProcedures.AddOrUpdate(key, storedProcedure, (k, v) => storedProcedure);
    }

    /// <summary>
    /// Removes a stored procedure from the semantic model.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to remove.</param>
    /// <returns>True if the stored procedure was removed; otherwise, false.</returns>
    public bool RemoveStoredProcedure(SemanticModelStoredProcedure storedProcedure)
    {
        var key = $"{storedProcedure.Schema}.{storedProcedure.Name}";
        return _loadedStoredProcedures.TryRemove(key, out _);
    }

    /// <summary>
    /// Finds a stored procedure in the semantic model by name and schema.
    /// </summary>
    /// <param name="schemaName">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The stored procedure if found; otherwise, null.</returns>
    public SemanticModelStoredProcedure? FindStoredProcedure(string schemaName, string storedProcedureName)
    {
        // First check if it's already loaded
        var key = $"{schemaName}.{storedProcedureName}";
        if (_loadedStoredProcedures.TryGetValue(key, out var sp))
        {
            return sp;
        }

        // Check if it exists in metadata
        if (_metadata.StoredProcedureIdentifiers.Contains(key))
        {
            // Load it on demand
            return LoadStoredProcedureAsync(schemaName, storedProcedureName).Result;
        }

        return null;
    }

    /// <summary>
    /// Accepts a visitor to traverse the semantic model.
    /// </summary>
    /// <param name="visitor">The visitor that will be used to traverse the model.</param>
    public void Accept(ISemanticModelVisitor visitor)
    {
        visitor.VisitSemanticModel(this);
        
        // Only visit loaded entities to avoid triggering lazy loading
        foreach (var table in _loadedTables.Values)
        {
            table.Accept(visitor);
        }

        foreach (var view in _loadedViews.Values)
        {
            view.Accept(visitor);
        }

        foreach (var sp in _loadedStoredProcedures.Values)
        {
            sp.Accept(visitor);
        }
    }
}