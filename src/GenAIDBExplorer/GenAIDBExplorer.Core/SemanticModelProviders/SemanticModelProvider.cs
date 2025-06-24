using GenAIDBExplorer.Core.Models.Progress;
using GenAIDBExplorer.Core.Models.Project;
using GenAIDBExplorer.Core.Models.SemanticModel;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Resources;

namespace GenAIDBExplorer.Core.SemanticModelProviders;

public sealed class SemanticModelProvider(
    IProject project,
    ISchemaRepository schemaRepository,
    ILogger<SemanticModelProvider> logger
) : ISemanticModelProvider
{
    private readonly IProject _project = project;
    private readonly ISchemaRepository _schemaRepository = schemaRepository;
    private readonly ILogger _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SemanticModelProvider).Assembly);

    /// <inheritdoc/>
    public SemanticModel CreateSemanticModel()
    {
        // Create the new SemanticModel instance to build
        var semanticModel = new SemanticModel(
            name: _project.Settings.Database.Name,
            source: _project.Settings.Database.ConnectionString,
            description: _project.Settings.Database.Description
        );

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> LoadSemanticModelAsync(DirectoryInfo modelPath)
    {
        _logger.LogInformation("{Message} '{ModelPath}'", _resourceManagerLogMessages.GetString("LoadingSemanticModel"), modelPath);

        var semanticModel = await CreateSemanticModel().LoadModelAsync(modelPath);

        _logger.LogInformation("{Message} '{SemanticModelName}'", _resourceManagerLogMessages.GetString("LoadedSemanticModelForDatabase"), semanticModel.Name);

        return semanticModel;
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> ExtractSemanticModelAsync()
    {
        return await ExtractSemanticModelAsync(null, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<SemanticModel> ExtractSemanticModelAsync(IProgress<SemanticModelExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("{Message} '{DatabaseName}'", _resourceManagerLogMessages.GetString("ExtractingModelForDatabase"), _project.Settings.Database.Name);

        var stopwatch = Stopwatch.StartNew();
        
        // Create the new SemanticModel instance to build
        var semanticModel = CreateSemanticModel();

        // Configure the parallel parallelOptions for the operation
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _project.Settings.Database.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        // Report initial progress
        progress?.Report(new SemanticModelExtractionProgress(0, 3, "Initialization", "Starting semantic model extraction"));

        cancellationToken.ThrowIfCancellationRequested();

        // Extract tables
        progress?.Report(new SemanticModelExtractionProgress(0, 3, "Tables", "Extracting tables from database"));
        await ExtractSemanticModelTablesAsync(semanticModel, parallelOptions, progress, stopwatch);

        cancellationToken.ThrowIfCancellationRequested();

        // Extract views
        progress?.Report(new SemanticModelExtractionProgress(1, 3, "Views", "Extracting views from database"));
        await ExtractSemanticModelViewsAsync(semanticModel, parallelOptions, progress, stopwatch);

        cancellationToken.ThrowIfCancellationRequested();

        // Extract stored procedures
        progress?.Report(new SemanticModelExtractionProgress(2, 3, "StoredProcedures", "Extracting stored procedures from database"));
        await ExtractSemanticModelStoredProceduresAsync(semanticModel, parallelOptions, progress, stopwatch);

        // Report completion
        progress?.Report(new SemanticModelExtractionProgress(3, 3, "Completed", "Semantic model extraction completed"));

        // return the semantic model Task
        return semanticModel;
    }

    /// <summary>
    /// Extracts the tables from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the tables will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <param name="progress">Optional progress reporter for tracking extraction progress.</param>
    /// <param name="stopwatch">Stopwatch for tracking elapsed time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelTablesAsync(SemanticModel semanticModel, ParallelOptions parallelOptions, IProgress<SemanticModelExtractionProgress>? progress, Stopwatch stopwatch)
    {
        // Get the tables from the database
        var tablesDictionary = await _schemaRepository.GetTablesAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelTables = new ConcurrentBag<SemanticModelTable>();

        var totalTables = tablesDictionary.Count;
        var processedTables = 0;

        // Construct the semantic model tables
        await Parallel.ForEachAsync(tablesDictionary.Values, parallelOptions, async (table, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{TableName}]", _resourceManagerLogMessages.GetString("AddingTableToSemanticModel"), table.SchemaName, table.TableName);

            var semanticModelTable = await _schemaRepository.CreateSemanticModelTableAsync(table).ConfigureAwait(false);
            semanticModelTables.Add(semanticModelTable);

            var currentCount = Interlocked.Increment(ref processedTables);
            progress?.Report(new SemanticModelExtractionProgress(
                0, 3, "Tables", 
                $"Processing table [{table.SchemaName}].[{table.TableName}] ({currentCount}/{totalTables})",
                EstimateRemainingTime(stopwatch.Elapsed, currentCount, totalTables + tablesDictionary.Count + tablesDictionary.Count)));
        });

        // Add the tables to the semantic model
        semanticModel.Tables.AddRange(semanticModelTables);
    }

    /// <summary>
    /// Extracts the views from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the views will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <param name="progress">Optional progress reporter for tracking extraction progress.</param>
    /// <param name="stopwatch">Stopwatch for tracking elapsed time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelViewsAsync(SemanticModel semanticModel, ParallelOptions parallelOptions, IProgress<SemanticModelExtractionProgress>? progress, Stopwatch stopwatch)
    {
        // Get the views from the database
        var viewsDictionary = await _schemaRepository.GetViewsAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelViews = new ConcurrentBag<SemanticModelView>();

        var totalViews = viewsDictionary.Count;
        var processedViews = 0;

        // Construct the semantic model views
        await Parallel.ForEachAsync(viewsDictionary.Values, parallelOptions, async (view, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{ViewName}]", _resourceManagerLogMessages.GetString("AddingViewToSemanticModel"), view.SchemaName, view.ViewName);

            var semanticModelView = await _schemaRepository.CreateSemanticModelViewAsync(view).ConfigureAwait(false);
            semanticModelViews.Add(semanticModelView);

            var currentCount = Interlocked.Increment(ref processedViews);
            progress?.Report(new SemanticModelExtractionProgress(
                1, 3, "Views", 
                $"Processing view [{view.SchemaName}].[{view.ViewName}] ({currentCount}/{totalViews})",
                EstimateRemainingTime(stopwatch.Elapsed, currentCount, totalViews)));
        });

        // Add the view to the semantic model
        semanticModel.Views.AddRange(semanticModelViews);
    }

    /// <summary>
    /// Extracts the stored procedures from the database and adds them to the semantic model asynchronously.
    /// </summary>
    /// <param name="semanticModel">The semantic model to which the stored procedures will be added.</param>
    /// <param name="parallelOptions">The parallel options for configuring the degree of parallelism.</param>
    /// <param name="progress">Optional progress reporter for tracking extraction progress.</param>
    /// <param name="stopwatch">Stopwatch for tracking elapsed time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExtractSemanticModelStoredProceduresAsync(SemanticModel semanticModel, ParallelOptions parallelOptions, IProgress<SemanticModelExtractionProgress>? progress, Stopwatch stopwatch)
    {
        // Get the stored procedures from the database
        var storedProceduresDictionary = await _schemaRepository.GetStoredProceduresAsync(_project.Settings.Database.Schema).ConfigureAwait(false);
        var semanticModelStoredProcedures = new ConcurrentBag<SemanticModelStoredProcedure>();

        var totalStoredProcedures = storedProceduresDictionary.Count;
        var processedStoredProcedures = 0;

        // Construct the semantic model stored procedures
        await Parallel.ForEachAsync(storedProceduresDictionary.Values, parallelOptions, async (storedProcedure, cancellationToken) =>
        {
            _logger.LogInformation("{Message} [{SchemaName}].[{StoredProcedureName}]", _resourceManagerLogMessages.GetString("AddingStoredProcedureToSemanticModel"), storedProcedure.SchemaName, storedProcedure.ProcedureName);

            var semanticModeStoredProcedure = await _schemaRepository.CreateSemanticModelStoredProcedureAsync(storedProcedure).ConfigureAwait(false);
            semanticModelStoredProcedures.Add(semanticModeStoredProcedure);

            var currentCount = Interlocked.Increment(ref processedStoredProcedures);
            progress?.Report(new SemanticModelExtractionProgress(
                2, 3, "StoredProcedures", 
                $"Processing stored procedure [{storedProcedure.SchemaName}].[{storedProcedure.ProcedureName}] ({currentCount}/{totalStoredProcedures})",
                EstimateRemainingTime(stopwatch.Elapsed, currentCount, totalStoredProcedures)));
        });

        // Add the stored procedures to the semantic model
        semanticModel.StoredProcedures.AddRange(semanticModelStoredProcedures);
    }

    /// <summary>
    /// Estimates the remaining time based on elapsed time and progress.
    /// </summary>
    /// <param name="elapsed">The time elapsed so far.</param>
    /// <param name="processed">The number of items processed.</param>
    /// <param name="total">The total number of items to process.</param>
    /// <returns>The estimated remaining time, or null if not enough data is available.</returns>
    private static TimeSpan? EstimateRemainingTime(TimeSpan elapsed, int processed, int total)
    {
        if (processed <= 0 || total <= 0 || processed >= total)
            return null;

        var avgTimePerItem = elapsed.TotalMilliseconds / processed;
        var remainingItems = total - processed;
        var remainingMs = avgTimePerItem * remainingItems;

        return TimeSpan.FromMilliseconds(remainingMs);
    }
}