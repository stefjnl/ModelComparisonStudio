using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.DTOs;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Application.UseCases;

/// <summary>
/// Use case for getting available models from all providers.
/// </summary>
public class GetAvailableModelsUseCase
{
    private readonly IComparisonOrchestrator _orchestrator;
    private readonly ILogger<GetAvailableModelsUseCase> _logger;

    public GetAvailableModelsUseCase(
        IComparisonOrchestrator orchestrator,
        ILogger<GetAvailableModelsUseCase> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all available models from all providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An available models DTO.</returns>
    public async Task<AvailableModelsDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting get available models use case");

        try
        {
            // Orchestrate getting available models
            var domainResponse = await _orchestrator.OrchestrateGetAvailableModelsAsync(
                cancellationToken);

            // Convert domain response to DTO
            var responseDto = AvailableModelsDto.FromDomainResponse(domainResponse);

            _logger.LogInformation("Get available models completed successfully. " +
                "NanoGPT: {NanoGPTCount} models, OpenRouter: {OpenRouterCount} models",
                responseDto.NanoGPT.ModelCount,
                responseDto.OpenRouter.ModelCount);

            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models");
            throw;
        }
    }
}
