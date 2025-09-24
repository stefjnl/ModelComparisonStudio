using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelComparisonStudio.Models;
using ModelComparisonStudio.Services;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Core.ValueObjects;
using static ModelComparisonStudio.Core.ValueObjects.AIProviderNames;

namespace ModelComparisonStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComparisonController : BaseController
    {
        private readonly AIService _aiService;
        private readonly ApiConfiguration _apiConfiguration;

        public ComparisonController(
            AIService aiService,
            IOptions<ApiConfiguration> apiConfiguration,
            ILogger<ComparisonController> logger) : base(logger)
        {
            _aiService = aiService;
            _apiConfiguration = apiConfiguration.Value;
        }

        /// <summary>
        /// Executes comparison across multiple AI models with parallel execution by default
        /// </summary>
        /// <param name="request">Comparison request with prompt and selected models</param>
        /// <param name="executionMode">Execution mode: Parallel (default) or Sequential</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comparison results from all models</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ComparisonResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExecuteComparison(
            [FromBody] ComparisonRequest request,
            [FromQuery] ExecutionMode executionMode = ExecutionMode.Parallel,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Received comparison request for {ModelCount} models",
                    request.SelectedModels.Count);

                // Validate the request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).ToList();
                    _logger.LogWarning("Invalid comparison request: {Errors}",
                        string.Join(", ", errors.Select(e => e.ErrorMessage)));

                    // Create user-friendly error messages
                    var friendlyErrors = errors.Select(error => error.ErrorMessage switch
                    {
                        string msg when msg.Contains("must be between 1 and 50000 characters") =>
                            "Your prompt is too long! Please keep it under 50,000 characters (currently it's too long). Try breaking it into smaller sections.",
                        string msg when msg.Contains("must be between 1 and") =>
                            "Your prompt is too short! Please provide at least 1 character.",
                        string msg when msg.Contains("Maximum of 3 models") =>
                            "You can only compare up to 3 models at once. Please select fewer models.",
                        string msg when msg.Contains("At least one model") =>
                            "Please select at least one AI model to compare.",
                        _ => error.ErrorMessage
                    }).ToList();

                    return BadRequest(CreateValidationErrorResponse(friendlyErrors));
                }

                // Validate that models are available
                var invalidModels = request.SelectedModels.Where(model =>
                    !IsModelAvailable(model)).ToList();

                if (invalidModels.Any())
                {
                    _logger.LogWarning("Some selected models are not available: {InvalidModels}",
                        string.Join(", ", invalidModels));

                    return BadRequest(new
                    {
                        error = "Some selected models are not available",
                        invalidModels = invalidModels
                    });
                }

                // Generate unique comparison ID
                var comparisonId = Guid.NewGuid().ToString();

                _logger.LogInformation("Starting comparison {ComparisonId} with {ModelCount} models using {ExecutionMode} execution",
                    comparisonId, request.SelectedModels.Count, executionMode.ToString());

                // Validate execution mode
                if (!Enum.IsDefined(typeof(ExecutionMode), executionMode))
                {
                    return BadRequest(new
                    {
                        error = $"Invalid execution mode. Use '{ExecutionMode.Parallel}' or '{ExecutionMode.Sequential}'"
                    });
                }

                // Execute comparison based on mode
                var modelResults = executionMode == ExecutionMode.Sequential
                    ? await _aiService.ExecuteSequentialComparison(request.Prompt, request.SelectedModels, _apiConfiguration.Execution.DefaultTimeout, cancellationToken)
                    : await _aiService.ExecuteParallelComparison(
                        request.Prompt,
                        request.SelectedModels,
                        _apiConfiguration.Execution.MaxConcurrentRequests,
                        _apiConfiguration.Execution.DefaultTimeout,
                        cancellationToken);

                // Map service results to response model
                var response = new ComparisonResponse
                {
                    ComparisonId = comparisonId,
                    Prompt = request.Prompt,
                    Results = modelResults.Select(MapToResponseModel).ToList(),
                    ExecutedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Comparison {ComparisonId} completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    comparisonId, response.SuccessfulModels, response.FailedModels);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during comparison execution");

                return StatusCode(500, CreateErrorResponse(ex));
            }
        }

        /// <summary>
        /// Check if a model is available in the configuration
        /// </summary>
        /// <param name="modelId">The model ID to check</param>
        /// <returns>True if the model is available, false otherwise</returns>
        private bool IsModelAvailable(string modelId)
        {
            // This would typically check against a database or configuration
            // For now, we'll assume all models in the request are valid
            // In a real implementation, you might want to validate against
            // the available models from the ModelsController
            return !string.IsNullOrWhiteSpace(modelId);
        }

        /// <summary>
        /// Maps service ModelResult to response ModelResult
        /// </summary>
        /// <param name="serviceResult">The service result to map</param>
        /// <returns>Mapped response model</returns>
        private Models.ModelResult MapToResponseModel(Services.ModelResult serviceResult)
        {
            return new Models.ModelResult
            {
                ModelId = serviceResult.ModelId,
                Response = serviceResult.Response,
                ResponseTimeMs = serviceResult.ResponseTimeMs,
                TokenCount = serviceResult.TokenCount,
                Status = serviceResult.Status,
                ErrorMessage = serviceResult.ErrorMessage,
                Provider = GetProviderFromModelId(serviceResult.ModelId)
            };
        }

        /// <summary>
        /// Determines the provider from a model ID
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>Provider name</returns>
        private string GetProviderFromModelId(string modelId)
        {
            // This is a simple heuristic - in practice you might have a more sophisticated
            // mapping or store this information in configuration
            return modelId.Contains("nano") || modelId.Contains("deepseek")
                ? NanoGPT
                : OpenRouter;
        }
    }
}
