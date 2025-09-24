using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Interfaces;

/// <summary>
/// Interface for the model comparison domain service.
/// </summary>
public interface IComparisonService
{
    /// <summary>
    /// Executes a comparison across multiple models.
    /// </summary>
    /// <param name="prompt">The prompt to send to all models.</param>
    /// <param name="modelIds">List of model IDs to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A comparison with results from all models.</returns>
    Task<Comparison> ExecuteComparisonAsync(
        Prompt prompt,
        IReadOnlyList<ModelId> modelIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a comparison with a single model.
    /// </summary>
    /// <param name="prompt">The prompt to send to the model.</param>
    /// <param name="modelId">The model ID to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A model result with the response and metadata.</returns>
    Task<ModelResult> ExecuteSingleModelAsync(
        Prompt prompt,
        ModelId modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all specified models are available.
    /// </summary>
    /// <param name="modelIds">List of model IDs to validate.</param>
    /// <returns>List of validation errors, empty if all models are valid.</returns>
    Task<IReadOnlyList<string>> ValidateModelsAsync(
        IReadOnlyList<ModelId> modelIds);

    /// <summary>
    /// Gets comparison statistics for analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison statistics.</returns>
    Task<ComparisonStatistics> GetComparisonStatisticsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for model comparison domain service that includes provider management.
/// </summary>
public interface IModelComparisonDomainService : IComparisonService
{
    /// <summary>
    /// Gets all available providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all available providers.</returns>
    Task<IReadOnlyList<Provider>> GetAvailableProvidersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available models across all providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all available model IDs.</returns>
    Task<IReadOnlyList<string>> GetAllAvailableModelsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets models for a specific provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of model IDs for the specified provider.</returns>
    Task<IReadOnlyList<string>> GetModelsForProviderAsync(
        string providerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model is available.
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the model is available, false otherwise.</returns>
    Task<bool> IsModelAvailableAsync(
        ModelId modelId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for model comparison orchestration.
/// </summary>
public interface IComparisonOrchestrator
{
    /// <summary>
    /// Orchestrates a complete comparison workflow.
    /// </summary>
    /// <param name="request">The comparison request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A comparison response with results.</returns>
    Task<ComparisonResponse> OrchestrateComparisonAsync(
        ComparisonRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates getting available models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available models response.</returns>
    Task<AvailableModelsResponse> OrchestrateGetAvailableModelsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for comparison operations.
/// </summary>
public class ComparisonRequest
{
    /// <summary>
    /// The prompt to send to all models.
    /// </summary>
    public Prompt Prompt { get; set; } = Prompt.CreateEmpty();

    /// <summary>
    /// List of model IDs to compare (1-3 models).
    /// </summary>
    public List<ModelId> SelectedModels { get; set; } = new();

    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (Prompt.IsEmpty())
        {
            errors.Add("Prompt is required");
        }

        if (SelectedModels.Count == 0)
        {
            errors.Add("At least one model must be selected");
        }

        if (SelectedModels.Count > 3)
        {
            errors.Add("Maximum of 3 models can be selected");
        }

        if (Prompt.Length > 50000)
        {
            errors.Add("Prompt must be between 1 and 50000 characters");
        }

        return errors;
    }
}

/// <summary>
/// Response model for comparison operations.
/// </summary>
public class ComparisonResponse
{
    /// <summary>
    /// Unique identifier for this comparison.
    /// </summary>
    public string ComparisonId { get; set; } = string.Empty;

    /// <summary>
    /// The original prompt that was sent to all models.
    /// </summary>
    public Prompt Prompt { get; set; } = Prompt.CreateEmpty();

    /// <summary>
    /// List of results from all models.
    /// </summary>
    public List<ModelResult> Results { get; set; } = new();

    /// <summary>
    /// Timestamp when the comparison was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of models processed.
    /// </summary>
    public int TotalModels => Results.Count;

    /// <summary>
    /// Number of successful model responses.
    /// </summary>
    public int SuccessfulModels => Results.Count(r => r.Status == ModelResultStatus.Success);

    /// <summary>
    /// Number of failed model responses.
    /// </summary>
    public int FailedModels => Results.Count(r => r.Status == ModelResultStatus.Error);

    /// <summary>
    /// Average response time across all models (in milliseconds).
    /// </summary>
    public double AverageResponseTime => Results.Any(r => r.Status == ModelResultStatus.Success)
        ? Results.Where(r => r.Status == ModelResultStatus.Success).Average(r => r.ResponseTimeMs)
        : 0;

    /// <summary>
    /// Total tokens used across all models.
    /// </summary>
    public int TotalTokens => Results.Sum(r => r.TokenCount ?? 0);
}

/// <summary>
/// Response model for available models operations.
/// </summary>
public class AvailableModelsResponse
{
    /// <summary>
    /// Models available from NanoGPT provider.
    /// </summary>
    public ProviderModels NanoGPT { get; set; } = new();

    /// <summary>
    /// Models available from OpenRouter provider.
    /// </summary>
    public ProviderModels OpenRouter { get; set; } = new();

    /// <summary>
    /// Total number of models across all providers.
    /// </summary>
    public int TotalModels => NanoGPT.ModelCount + OpenRouter.ModelCount;
}

/// <summary>
/// Model information for a specific provider.
/// </summary>
public class ProviderModels
{
    /// <summary>
    /// The provider name.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The base URL for the provider.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// List of available model IDs.
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Number of available models.
    /// </summary>
    public int ModelCount => Models.Count;
}
