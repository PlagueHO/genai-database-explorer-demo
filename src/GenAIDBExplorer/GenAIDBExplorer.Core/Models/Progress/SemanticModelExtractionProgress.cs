namespace GenAIDBExplorer.Core.Models.Progress;

/// <summary>
/// Represents progress information for semantic model extraction operations.
/// </summary>
public class SemanticModelExtractionProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticModelExtractionProgress"/> class.
    /// </summary>
    /// <param name="currentStep">The current step number (0-based).</param>
    /// <param name="totalSteps">The total number of steps in the operation.</param>
    /// <param name="currentPhase">The current phase of extraction.</param>
    /// <param name="message">A descriptive message about the current operation.</param>
    /// <param name="estimatedTimeRemaining">The estimated time remaining for the operation.</param>
    public SemanticModelExtractionProgress(
        int currentStep, 
        int totalSteps, 
        string currentPhase, 
        string message, 
        TimeSpan? estimatedTimeRemaining = null)
    {
        CurrentStep = currentStep;
        TotalSteps = totalSteps;
        CurrentPhase = currentPhase;
        Message = message;
        EstimatedTimeRemaining = estimatedTimeRemaining;
    }

    /// <summary>
    /// Gets the current step number (0-based).
    /// </summary>
    public int CurrentStep { get; }

    /// <summary>
    /// Gets the total number of steps in the operation.
    /// </summary>
    public int TotalSteps { get; }

    /// <summary>
    /// Gets the current phase of extraction (e.g., "Tables", "Views", "StoredProcedures").
    /// </summary>
    public string CurrentPhase { get; }

    /// <summary>
    /// Gets a descriptive message about the current operation.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the estimated time remaining for the operation, if available.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; }

    /// <summary>
    /// Gets the completion percentage (0-100).
    /// </summary>
    public double PercentageComplete => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
}