namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Represents a semantic model with lazy loading capabilities.
/// </summary>
public interface ILazySemanticModel : ISemanticModel
{
    /// <summary>
    /// Loads a table on-demand by schema and table name.
    /// </summary>
    /// <param name="schema">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The loaded semantic model table.</returns>
    Task<SemanticModelTable> LoadTableAsync(string schema, string tableName);
    
    /// <summary>
    /// Loads a view on-demand by schema and view name.
    /// </summary>
    /// <param name="schema">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The loaded semantic model view.</returns>
    Task<SemanticModelView> LoadViewAsync(string schema, string viewName);
    
    /// <summary>
    /// Loads a stored procedure on-demand by schema and stored procedure name.
    /// </summary>
    /// <param name="schema">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>The loaded semantic model stored procedure.</returns>
    Task<SemanticModelStoredProcedure> LoadStoredProcedureAsync(string schema, string storedProcedureName);
    
    /// <summary>
    /// Loads multiple tables on-demand by their identifiers.
    /// </summary>
    /// <param name="tableIds">The table identifiers in "schema.name" format.</param>
    /// <returns>The loaded semantic model tables.</returns>
    Task<List<SemanticModelTable>> LoadTablesAsync(IEnumerable<string> tableIds);
    
    /// <summary>
    /// Loads multiple views on-demand by their identifiers.
    /// </summary>
    /// <param name="viewIds">The view identifiers in "schema.name" format.</param>
    /// <returns>The loaded semantic model views.</returns>
    Task<List<SemanticModelView>> LoadViewsAsync(IEnumerable<string> viewIds);
    
    /// <summary>
    /// Loads multiple stored procedures on-demand by their identifiers.
    /// </summary>
    /// <param name="storedProcedureIds">The stored procedure identifiers in "schema.name" format.</param>
    /// <returns>The loaded semantic model stored procedures.</returns>
    Task<List<SemanticModelStoredProcedure>> LoadStoredProceduresAsync(IEnumerable<string> storedProcedureIds);
    
    /// <summary>
    /// Gets a value indicating whether a table is currently loaded in memory.
    /// </summary>
    /// <param name="schema">The schema name of the table.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>True if the table is loaded; otherwise, false.</returns>
    bool IsTableLoaded(string schema, string tableName);
    
    /// <summary>
    /// Gets a value indicating whether a view is currently loaded in memory.
    /// </summary>
    /// <param name="schema">The schema name of the view.</param>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>True if the view is loaded; otherwise, false.</returns>
    bool IsViewLoaded(string schema, string viewName);
    
    /// <summary>
    /// Gets a value indicating whether a stored procedure is currently loaded in memory.
    /// </summary>
    /// <param name="schema">The schema name of the stored procedure.</param>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>True if the stored procedure is loaded; otherwise, false.</returns>
    bool IsStoredProcedureLoaded(string schema, string storedProcedureName);
}