using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.DTOs;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Application.UseCases;

/// <summary>
/// Use case for executing model comparisons.
/// </summary>
public class ExecuteComparisonUseCase
{
    private readonly IComparisonOrchestrator _orchestrator;
    private readonly ILogger<ExecuteComparisonUseCase> _logger;

    public ExecuteComparisonUseCase(
        IComparisonOrchestrator orchestrator,
        ILogger<ExecuteComparisonUseCase> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a model comparison.
    /// </summary>
    /// <param name="requestDto">The comparison request DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A comparison response DTO.</returns>
    public async Task<ComparisonResponseDto> ExecuteAsync(
        ComparisonRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        if (requestDto == null)
        {
            throw new ArgumentNullException(nameof(requestDto));
        }

        _logger.LogInformation("Starting comparison execution use case");

        try
        {
            // Convert DTO to domain request
            var domainRequest = requestDto.ToDomainRequest();

            // Validate the request
            var validationErrors = domainRequest.Validate();
            if (validationErrors.Any())
            {
                var errorMessage = $"Validation failed: {string.Join(", ", validationErrors)}";
                _logger.LogWarning(errorMessage);
                throw new ValidationException(errorMessage);
            }

            // Orchestrate the comparison
            var domainResponse = await _orchestrator.OrchestrateComparisonAsync(
                domainRequest,
                cancellationToken);

            // Convert domain response to DTO
            var responseDto = ComparisonResponseDto.FromDomainComparison(
                await ConvertDomainResponseToComparison(domainResponse));

            _logger.LogInformation("Comparison execution completed successfully for {ModelCount} models",
                requestDto.SelectedModels.Count);

            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing comparison use case");
            throw;
        }
    }

    /// <summary>
    /// Converts a domain comparison response to a comparison entity.
    /// This is a temporary method until we implement proper repository integration.
    /// </summary>
    /// <param name="domainResponse">The domain response.</param>
    /// <returns>A comparison entity.</returns>
    private async Task<Core.Entities.Comparison> ConvertDomainResponseToComparison(
        Core.Interfaces.ComparisonResponse domainResponse)
    {
        var comparison = Core.Entities.Comparison.Create(domainResponse.Prompt.Content);

        foreach (var result in domainResponse.Results)
        {
            var modelResult = Core.Entities.ModelResult.Create(
                result.ModelId,
                result.Response,
                result.ResponseTimeMs,
                result.TokenCount,
                result.Status,
                result.ErrorMessage,
                result.Provider);

            comparison.AddResult(modelResult);
        }

        return comparison;
    }
}
