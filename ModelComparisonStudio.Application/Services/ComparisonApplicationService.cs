using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.DTOs;
using ModelComparisonStudio.Application.UseCases;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Core.ValueObjects;
using static ModelComparisonStudio.Core.ValueObjects.AIProviderNames;
using static ModelComparisonStudio.Core.ValueObjects.AIProviderUrls;

namespace ModelComparisonStudio.Application.Services;

/// <summary>
/// Application service for model comparison operations.
/// </summary>
public class ComparisonApplicationService : IComparisonOrchestrator
{
    private readonly IModelComparisonDomainService _domainService;
    private readonly ILogger<ComparisonApplicationService> _logger;

    public ComparisonApplicationService(
        IModelComparisonDomainService domainService,
        ILogger<ComparisonApplicationService> logger)
    {
        _domainService = domainService ?? throw new ArgumentNullException(nameof(domainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Orchestrates a complete comparison workflow.
    /// </summary>
    public async Task<Core.Interfaces.ComparisonResponse> OrchestrateComparisonAsync(
        Core.Interfaces.ComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation("Starting comparison orchestration for {ModelCount} models",
            request.SelectedModels.Count);

        try
        {
            // Convert domain request to domain objects
            var prompt = request.Prompt;
            var modelIds = request.SelectedModels.ToList();

            // Execute the comparison using domain service
            var comparison = await _domainService.ExecuteComparisonAsync(
                prompt,
                modelIds,
                cancellationToken);

            // Convert comparison to domain response
            var response = new Core.Interfaces.ComparisonResponse
            {
                ComparisonId = comparison.Id,
                Prompt = prompt,
                Results = comparison.Results
                    .Select(result => Core.Entities.ModelResult.CreateSuccess(
                        result.ModelId,
                        result.Response,
                        result.ResponseTimeMs,
                        result.TokenCount,
                        result.Provider))
                    .ToList(),
                ExecutedAt = comparison.ExecutedAt
            };

            _logger.LogInformation("Comparison orchestration completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating comparison");
            throw;
        }
    }

    /// <summary>
    /// Orchestrates getting available models.
    /// </summary>
    public async Task<Core.Interfaces.AvailableModelsResponse> OrchestrateGetAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting get available models orchestration");

        try
        {
            // Get all available providers
            var providers = await _domainService.GetAvailableProvidersAsync(cancellationToken);

            // Group models by provider
            var nanoGptProvider = providers.FirstOrDefault(p => p.Name.Equals(AIProviderNames.NanoGPT, StringComparison.OrdinalIgnoreCase));
            var openRouterProvider = providers.FirstOrDefault(p => p.Name.Equals(AIProviderNames.OpenRouter, StringComparison.OrdinalIgnoreCase));

            // Create response
            var nanoGptModels = nanoGptProvider?.AvailableModels?.ToList() ?? new List<string>();
            var openRouterModels = openRouterProvider?.AvailableModels?.ToList() ?? new List<string>();

            var response = new Core.Interfaces.AvailableModelsResponse
            {
                NanoGPT = new Core.Interfaces.ProviderModels
                {
                    Provider = nanoGptProvider?.Name ?? NanoGPT,
                    BaseUrl = nanoGptProvider?.BaseUrl ?? NanoGPTBaseUrl,
                    Models = nanoGptModels
                },
                OpenRouter = new Core.Interfaces.ProviderModels
                {
                    Provider = openRouterProvider?.Name ?? OpenRouter,
                    BaseUrl = openRouterProvider?.BaseUrl ?? OpenRouterBaseUrl,
                    Models = openRouterModels
                }
            };

            _logger.LogInformation("Get available models orchestration completed");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error orchestrating get available models");
            throw;
        }
    }
}
