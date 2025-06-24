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
    Task<Dictionary<TableInfo, List<SemanticModelColumn>>> GetColumnsForTablesAsync(IEnumerable<TableInfo> tables);
    Task<List<SemanticModelTable>> CreateSemanticModelTablesAsync(IEnumerable<TableInfo> tables);
    Task<Dictionary<TableInfo, List<Dictionary<string, object>>>> GetSampleDataForTablesAsync(IEnumerable<TableInfo> tables, int numberOfRecords = 5, bool selectRandom = false);
}
