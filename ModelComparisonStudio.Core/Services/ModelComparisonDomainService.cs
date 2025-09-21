using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Services;

/// <summary>
/// Domain service for model comparison operations.
/// </summary>
public class ModelComparisonDomainService : IModelComparisonDomainService
{
    private readonly IAIProviderManager _providerManager;
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<ModelComparisonDomainService> _logger;

    public ModelComparisonDomainService(
        IAIProviderManager providerManager,
        IModelRepository modelRepository,
        ILogger<ModelComparisonDomainService> logger)
    {
        _providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a comparison across multiple models.
    /// </summary>
    public async Task<Comparison> ExecuteComparisonAsync(
        Prompt prompt,
        IReadOnlyList<ModelId> modelIds,
        CancellationToken cancellationToken = default)
    {
        if (prompt == null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        if (modelIds == null || modelIds.Count == 0)
        {
            throw new ArgumentException("At least one model ID must be provided.", nameof(modelIds));
        }

        _logger.LogInformation("Starting comparison with {ModelCount} models", modelIds.Count);

        // Validate all models are available
        var validationErrors = await ValidateModelsAsync(modelIds);
        if (validationErrors.Any())
        {
            var errorMessage = $"Model validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Create the comparison
        var comparison = Comparison.Create(prompt.Content);
        _logger.LogInformation("Created comparison {ComparisonId}", comparison.Id);

        // Execute requests for each model
        foreach (var modelId in modelIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Comparison cancelled for model {ModelId}", modelId);
                break;
            }

            try
            {
                _logger.LogInformation("Processing model {ModelId}", modelId);
                var result = await ExecuteSingleModelAsync(prompt, modelId, cancellationToken);
                comparison.AddResult(result);
                _logger.LogInformation("Completed model {ModelId} with status {Status} in {ResponseTime}ms",
                    modelId, result.Status, result.ResponseTimeMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing model {ModelId}", modelId);
                var errorResult = ModelResult.CreateError(modelId.Value, ex.Message, 0);
                comparison.AddResult(errorResult);
            }
        }

        // Save the comparison
        await _modelRepository.SaveComparisonAsync(comparison, cancellationToken);

        _logger.LogInformation("Comparison {ComparisonId} completed. Success: {SuccessCount}, Failed: {FailedCount}",
            comparison.Id, comparison.SuccessfulModels, comparison.FailedModels);

        return comparison;
    }

    /// <summary>
    /// Executes a comparison with a single model.
    /// </summary>
    public async Task<ModelResult> ExecuteSingleModelAsync(
        Prompt prompt,
        ModelId modelId,
        CancellationToken cancellationToken = default)
    {
        if (prompt == null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        if (modelId == null)
        {
            throw new ArgumentNullException(nameof(modelId));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting single model execution for {ModelId}", modelId);

            // Get the provider for this model
            var provider = await _providerManager.GetProviderForModelAsync(modelId);
            if (provider == null)
            {
                throw new InvalidOperationException($"No provider found for model {modelId}");
            }

            _logger.LogInformation("Using provider {ProviderName} for model {ModelId}", provider.Name, modelId);

            // Execute the request
            var result = await provider.ExecuteRequestAsync(modelId, prompt, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Single model execution completed for {ModelId} in {ElapsedMs}ms",
                modelId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing single model {ModelId} after {ElapsedMs}ms",
                modelId, stopwatch.ElapsedMilliseconds);

            return ModelResult.CreateError(modelId.Value, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Validates that all specified models are available.
    /// </summary>
    public async Task<IReadOnlyList<string>> ValidateModelsAsync(
        IReadOnlyList<ModelId> modelIds)
    {
        if (modelIds == null || modelIds.Count == 0)
        {
            return new[] { "No models specified for validation" };
        }

        var errors = new List<string>();

        foreach (var modelId in modelIds)
        {
            var isAvailable = await _providerManager.IsModelAvailableAsync(modelId);
            if (!isAvailable)
            {
                errors.Add($"Model {modelId} is not available");
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets comparison statistics for analysis.
    /// </summary>
    public async Task<ComparisonStatistics> GetComparisonStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetStatisticsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all available providers.
    /// </summary>
    public async Task<IReadOnlyList<Provider>> GetAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerManager.GetAllProvidersAsync();
        return providers.Select(p => Provider.CreateForMapping(
            p.Name,
            p.BaseUrl,
            p.AvailableModels.ToList()
        )).ToList();
    }

    /// <summary>
    /// Gets all available models across all providers.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAllAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _providerManager.GetAllAvailableModelsAsync();
    }

    /// <summary>
    /// Gets models for a specific provider.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetModelsForProviderAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return Array.Empty<string>();
        }

        var provider = await _providerManager.GetProviderByNameAsync(providerName);
        return provider?.AvailableModels ?? Array.Empty<string>();
    }

    /// <summary>
    /// Checks if a model is available.
    /// </summary>
    public async Task<bool> IsModelAvailableAsync(
        ModelId modelId,
        CancellationToken cancellationToken = default)
    {
        if (modelId == null)
        {
            return false;
        }

        return await _providerManager.IsModelAvailableAsync(modelId);
    }
}
