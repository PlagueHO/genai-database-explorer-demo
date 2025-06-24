using GenAIDBExplorer.Core.Models.Database;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

/// <summary>
/// Decorator for ISchemaRepository that adds caching capabilities.
/// </summary>
public sealed class CachedSchemaRepository : ISchemaRepository
{
    private readonly ISchemaRepository _innerRepository;
    private readonly ISemanticModelCache _cache;
    private readonly SemanticModelCacheSettings _cacheSettings;
    private readonly ILogger<CachedSchemaRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedSchemaRepository"/> class.
    /// </summary>
    /// <param name="innerRepository">The inner schema repository to decorate.</param>
    /// <param name="cache">The cache implementation.</param>
    /// <param name="project">The project configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public CachedSchemaRepository(
        ISchemaRepository innerRepository,
        ISemanticModelCache cache,
        IProject project,
        ILogger<CachedSchemaRepository> logger)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(project);
        _cacheSettings = project.Settings.SemanticModel.Cache;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, TableInfo>> GetTablesAsync(string? schema = null)
    {
        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetTablesAsync(schema).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("tables", schema);
        
        return await _cache.GetOrSetTablesAsync(
            cacheKey,
            () => _innerRepository.GetTablesAsync(schema),
            _cacheSettings.TablesTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, ViewInfo>> GetViewsAsync(string? schema = null)
    {
        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetViewsAsync(schema).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("views", schema);
        
        return await _cache.GetOrSetViewsAsync(
            cacheKey,
            () => _innerRepository.GetViewsAsync(schema),
            _cacheSettings.ViewsTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, StoredProcedureInfo>> GetStoredProceduresAsync(string? schema = null)
    {
        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetStoredProceduresAsync(schema).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("storedprocedures", schema);
        
        return await _cache.GetOrSetStoredProceduresAsync(
            cacheKey,
            () => _innerRepository.GetStoredProceduresAsync(schema),
            _cacheSettings.StoredProceduresTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> GetViewDefinitionAsync(ViewInfo view)
    {
        ArgumentNullException.ThrowIfNull(view);

        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetViewDefinitionAsync(view).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("viewdefinition", view.SchemaName, view.ViewName);
        
        return await _cache.GetOrSetViewDefinitionAsync(
            cacheKey,
            () => _innerRepository.GetViewDefinitionAsync(view),
            _cacheSettings.ViewDefinitionsTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SemanticModelTable> CreateSemanticModelTableAsync(TableInfo table)
    {
        // Semantic model creation is not cached as it involves business logic and transformations
        // that may change based on project settings
        return await _innerRepository.CreateSemanticModelTableAsync(table).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SemanticModelView> CreateSemanticModelViewAsync(ViewInfo view)
    {
        // Semantic model creation is not cached as it involves business logic and transformations
        // that may change based on project settings
        return await _innerRepository.CreateSemanticModelViewAsync(view).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SemanticModelStoredProcedure> CreateSemanticModelStoredProcedureAsync(StoredProcedureInfo storedProcedure)
    {
        // Semantic model creation is not cached as it involves business logic and transformations
        // that may change based on project settings
        return await _innerRepository.CreateSemanticModelStoredProcedureAsync(storedProcedure).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SemanticModelColumn>> GetColumnsForTableAsync(TableInfo table)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetColumnsForTableAsync(table).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("tablecolumns", table.SchemaName, table.TableName);
        
        return await _cache.GetOrSetColumnsAsync(
            cacheKey,
            () => _innerRepository.GetColumnsForTableAsync(table),
            _cacheSettings.ColumnsTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SemanticModelColumn>> GetColumnsForViewAsync(ViewInfo view)
    {
        ArgumentNullException.ThrowIfNull(view);

        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetColumnsForViewAsync(view).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("viewcolumns", view.SchemaName, view.ViewName);
        
        return await _cache.GetOrSetColumnsAsync(
            cacheKey,
            () => _innerRepository.GetColumnsForViewAsync(view),
            _cacheSettings.ColumnsTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<Dictionary<string, object>>> GetSampleTableDataAsync(TableInfo tableInfo, int numberOfRecords = 5, bool selectRandom = false)
    {
        ArgumentNullException.ThrowIfNull(tableInfo);

        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetSampleTableDataAsync(tableInfo, numberOfRecords, selectRandom).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("tablesampledata", tableInfo.SchemaName, tableInfo.TableName, numberOfRecords.ToString(), selectRandom.ToString());
        
        return await _cache.GetOrSetSampleDataAsync(
            cacheKey,
            () => _innerRepository.GetSampleTableDataAsync(tableInfo, numberOfRecords, selectRandom),
            _cacheSettings.SampleDataTtl).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<Dictionary<string, object>>> GetSampleViewDataAsync(ViewInfo viewInfo, int numberOfRecords = 5, bool selectRandom = false)
    {
        ArgumentNullException.ThrowIfNull(viewInfo);

        if (!_cacheSettings.Enabled)
        {
            return await _innerRepository.GetSampleViewDataAsync(viewInfo, numberOfRecords, selectRandom).ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey("viewsampledata", viewInfo.SchemaName, viewInfo.ViewName, numberOfRecords.ToString(), selectRandom.ToString());
        
        return await _cache.GetOrSetSampleDataAsync(
            cacheKey,
            () => _innerRepository.GetSampleViewDataAsync(viewInfo, numberOfRecords, selectRandom),
            _cacheSettings.SampleDataTtl).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a cache key from the provided components.
    /// </summary>
    /// <param name="components">The components to combine into a cache key.</param>
    /// <returns>A cache key string.</returns>
    private static string GenerateCacheKey(params string?[] components)
    {
        // Filter out null/empty components and join with a delimiter
        var nonEmptyComponents = components.Where(c => !string.IsNullOrEmpty(c));
        return string.Join(":", nonEmptyComponents);
    }
}