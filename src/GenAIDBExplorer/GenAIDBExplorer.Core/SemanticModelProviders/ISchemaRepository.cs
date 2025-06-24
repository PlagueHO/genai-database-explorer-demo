using GenAIDBExplorer.Core.Models.SemanticModel;
using GenAIDBExplorer.Core.Models.Database;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

public interface ISchemaRepository
{
    Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null);
    Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null);
    Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null);
    Task<string> GetViewDefinitionAsync(ViewInfo view);
    Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table);
    Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view);
    Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure);
    Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table);
    Task<List<SemanticModelColumn>> GetColumnsForViewAsync(ViewInfo view);
    Task<List<Dictionary<string, object>>> GetSampleTableDataAsync(TableInfo tableInfo, int numberOfRecords = 5, bool selectRandom = false);
    Task<List<Dictionary<string, object>>> GetSampleViewDataAsync(ViewInfo viewInfo, int numberOfRecords = 5, bool selectRandom = false);
    
    // Batch operations to reduce database round trips
    /// <summary>
    /// Retrieves columns for multiple tables in a single database operation to minimize round trips.
    /// </summary>
    /// <param name="tables">The collection of table info objects for which to retrieve columns.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary mapping each table to its columns.</returns>
    Task<Dictionary<TableInfo, List<SemanticModelColumn>>> GetColumnsForTablesAsync(IEnumerable<TableInfo> tables);
    
    /// <summary>
    /// Creates semantic model tables for multiple tables using optimized batch operations to reduce database round trips.
    /// </summary>
    /// <param name="tables">The collection of table info objects for which to create semantic models.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a list of created semantic model tables.</returns>
    Task<List<SemanticModelTable>> CreateSemanticModelTablesAsync(IEnumerable<TableInfo> tables);
    
    /// <summary>
    /// Retrieves sample data for multiple tables in parallel operations for better efficiency.
    /// </summary>
    /// <param name="tables">The collection of table info objects for which to retrieve sample data.</param>
    /// <param name="numberOfRecords">The number of records to retrieve per table.</param>
    /// <param name="selectRandom">Whether to select a random sample of records.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a dictionary mapping each table to its sample data.</returns>
    Task<Dictionary<TableInfo, List<Dictionary<string, object>>>> GetSampleDataForTablesAsync(IEnumerable<TableInfo> tables, int numberOfRecords = 5, bool selectRandom = false);
}
