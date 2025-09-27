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
        private readonly ModelComparisonStudio.Infrastructure.Services.QueryPerformanceMonitor _performanceMonitor;

        public ComparisonController(
            AIService aiService,
            IOptions<ApiConfiguration> apiConfiguration,
            ModelComparisonStudio.Infrastructure.Services.QueryPerformanceMonitor performanceMonitor,
            ILogger<ComparisonController> logger) : base(logger)
        {
            _aiService = aiService;
            _apiConfiguration = apiConfiguration.Value;
            _performanceMonitor = performanceMonitor;
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

                // Determine appropriate timeout based on prompt length and complexity
                var timeout = DetermineOptimalTimeout(request.Prompt);
                _logger.LogInformation("Using timeout of {TimeoutSeconds} seconds for comparison with {PromptLength} characters",
                    timeout.TotalSeconds, request.Prompt.Length);

                // Execute comparison based on mode
                var modelResults = executionMode == ExecutionMode.Sequential
                    ? await _aiService.ExecuteSequentialComparison(request.Prompt, request.SelectedModels, timeout, cancellationToken)
                    : await _aiService.ExecuteParallelComparison(
                        request.Prompt,
                        request.SelectedModels,
                        _apiConfiguration.Execution.MaxConcurrentRequests,
                        timeout,
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
        /// Gets performance metrics for AI model operations
        /// </summary>
        /// <returns>Performance statistics for all tracked AI operations</returns>
        [HttpGet("performance")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetPerformanceMetrics()
        {
            try
            {
                var stats = _performanceMonitor.GetPerformanceStats();
                var aiStats = stats.Where(s => s.Key.StartsWith("AI-")).ToDictionary(s => s.Key, s => s.Value);

                if (!aiStats.Any())
                {
                    return Ok(new
                    {
                        message = "No AI performance data available yet",
                        timestamp = DateTime.UtcNow
                    });
                }

                var summary = new
                {
                    totalOperations = aiStats.Sum(s => s.Value.ExecutionCount),
                    averageExecutionTime = aiStats.Any() ? aiStats.Average(s => s.Value.AverageExecutionTimeMs) : 0,
                    slowestModel = aiStats.OrderByDescending(s => s.Value.AverageExecutionTimeMs).FirstOrDefault().Key,
                    fastestModel = aiStats.OrderBy(s => s.Value.AverageExecutionTimeMs).FirstOrDefault().Key,
                    models = aiStats.ToDictionary(
                        s => s.Key,
                        s => new
                        {
                            averageTime = Math.Round(s.Value.AverageExecutionTimeMs, 2),
                            minTime = s.Value.MinExecutionTimeMs,
                            maxTime = s.Value.MaxExecutionTimeMs,
                            executionCount = s.Value.ExecutionCount
                        }
                    ),
                    timestamp = DateTime.UtcNow
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics");
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
        /// Determines the optimal timeout based on prompt characteristics
        /// </summary>
        /// <param name="prompt">The prompt text</param>
        /// <returns>Appropriate timeout duration</returns>
        private TimeSpan DetermineOptimalTimeout(string prompt)
        {
            var promptLength = prompt.Length;
            var wordCount = prompt.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Use extended timeout for long prompts or complex tasks
            if (promptLength > 10000 || wordCount > 1500)
            {
                _logger.LogInformation("Using extended timeout for long prompt: {PromptLength} chars, {WordCount} words",
                    promptLength, wordCount);
                return _apiConfiguration.Execution.ExtendedTimeout;
            }

            // Use standard timeout for medium-length prompts
            if (promptLength > 2000 || wordCount > 300)
            {
                _logger.LogInformation("Using standard timeout for medium prompt: {PromptLength} chars, {WordCount} words",
                    promptLength, wordCount);
                return _apiConfiguration.Execution.StandardTimeout;
            }

            // Use quick timeout for short prompts
            _logger.LogInformation("Using quick timeout for short prompt: {PromptLength} chars, {WordCount} words",
                promptLength, wordCount);
            return _apiConfiguration.Execution.QuickTimeout;
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
