using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelComparisonStudio.Configuration;

namespace ModelComparisonStudio.Services
{
    public class AIService
    {
        private readonly ApiConfiguration _apiConfiguration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;

        public AIService(
            IOptions<ApiConfiguration> apiConfiguration,
            HttpClient httpClient,
            ILogger<AIService> logger)
        {
            _apiConfiguration = apiConfiguration.Value;
            _httpClient = httpClient;
            _logger = logger;
            
            // Enhanced diagnostic logging
            _logger.LogInformation("=== AIService Configuration Diagnostic ===");
            _logger.LogInformation("NanoGPT Configuration: {@NanoGPT}", _apiConfiguration.NanoGPT);
            _logger.LogInformation("OpenRouter Configuration: {@OpenRouter}", _apiConfiguration.OpenRouter);
            
            if (_apiConfiguration.NanoGPT != null)
            {
                _logger.LogInformation("NanoGPT API Key: {ApiKeyLength} characters",
                    string.IsNullOrEmpty(_apiConfiguration.NanoGPT.ApiKey) ? 0 : _apiConfiguration.NanoGPT.ApiKey.Length);
                _logger.LogInformation("NanoGPT Available Models: {ModelCount}",
                    _apiConfiguration.NanoGPT.AvailableModels?.Length ?? 0);
            }
            
            if (_apiConfiguration.OpenRouter != null)
            {
                _logger.LogInformation("OpenRouter API Key: {ApiKeyLength} characters",
                    string.IsNullOrEmpty(_apiConfiguration.OpenRouter.ApiKey) ? 0 : _apiConfiguration.OpenRouter.ApiKey.Length);
                _logger.LogInformation("OpenRouter Available Models: {ModelCount}",
                    _apiConfiguration.OpenRouter.AvailableModels?.Length ?? 0);
            }
            _logger.LogInformation("=== End Configuration Diagnostic ===");
        }

        /// <summary>
        /// Analyzes code using the specified AI model
        /// </summary>
        /// <param name="prompt">The prompt to send to the model</param>
        /// <param name="modelId">The model ID to use (e.g., "openai/gpt-4o-mini")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis result with response and metadata</returns>
        public async Task<AnalysisResult> AnalyzeCodeAsync(
            string prompt,
            string modelId,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting analysis with model {ModelId}", modelId);
                _logger.LogInformation("Configuration loaded - NanoGPT API Key: {ApiKey}, OpenRouter API Key: {OpenRouterApiKey}",
                    _apiConfiguration.NanoGPT?.ApiKey ?? "NULL",
                    _apiConfiguration.OpenRouter?.ApiKey ?? "NULL");

                // Determine which provider to use based on the model ID
                var (provider, apiKey, baseUrl) = GetProviderInfo(modelId);
                _logger.LogInformation("Provider info result - Provider: {Provider}, API Key length: {ApiKeyLength}, Base URL: {BaseUrl}",
                    provider, apiKey?.Length ?? 0, baseUrl);

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException($"API key not configured for {provider}");
                }

                // Prepare the request
                var request = new
                {
                    model = modelId,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Set up headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://modelcomparisonstudio.com");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "Model Comparison Studio");

                // Make the API call
                var response = await _httpClient.PostAsync($"{baseUrl}/chat/completions", content, cancellationToken);

                stopwatch.Stop();
                var responseTime = stopwatch.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("API call failed for model {ModelId}: {StatusCode} - {ErrorContent}",
                        modelId, response.StatusCode, errorContent);

                    return new AnalysisResult
                    {
                        ModelId = modelId,
                        Response = $"Error: {response.StatusCode} - {response.ReasonPhrase}",
                        ResponseTimeMs = responseTime,
                        Status = "error",
                        ErrorMessage = errorContent
                    };
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenRouterResponse>(jsonResponse);

                if (apiResponse?.Choices?.Length == 0)
                {
                    throw new InvalidOperationException("No response choices returned from API");
                }

                var result = new AnalysisResult
                {
                    ModelId = modelId,
                    Response = apiResponse.Choices[0].Message.Content,
                    ResponseTimeMs = responseTime,
                    TokenCount = apiResponse.Usage?.TotalTokens,
                    Status = "success"
                };

                _logger.LogInformation("Analysis completed for model {ModelId} in {ResponseTime}ms with {TokenCount} tokens",
                    modelId, responseTime, apiResponse.Usage?.TotalTokens ?? 0);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error analyzing code with model {ModelId}", modelId);

                return new AnalysisResult
                {
                    ModelId = modelId,
                    Response = $"Error: {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Status = "error",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Executes sequential comparison across multiple models
        /// </summary>
        /// <param name="prompt">The prompt to send to all models</param>
        /// <param name="modelIds">List of model IDs to compare</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of results from all models</returns>
        public async Task<List<ModelResult>> ExecuteSequentialComparison(
            string prompt,
            List<string> modelIds,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ModelResult>();
            _logger.LogInformation("Starting sequential comparison for {ModelCount} models", modelIds.Count);

            foreach (var modelId in modelIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Sequential comparison cancelled");
                    break;
                }

                try
                {
                    _logger.LogInformation("Processing model {ModelIndex}/{ModelCount}: {ModelId}",
                        results.Count + 1, modelIds.Count, modelId);

                    var analysisResult = await AnalyzeCodeAsync(prompt, modelId, cancellationToken);

                    var modelResult = new ModelResult
                    {
                        ModelId = analysisResult.ModelId,
                        Response = analysisResult.Response,
                        ResponseTimeMs = analysisResult.ResponseTimeMs,
                        TokenCount = analysisResult.TokenCount,
                        Status = analysisResult.Status,
                        ErrorMessage = analysisResult.ErrorMessage
                    };

                    results.Add(modelResult);

                    _logger.LogInformation("Completed model {ModelId} with status {Status} in {ResponseTime}ms",
                        modelId, modelResult.Status, modelResult.ResponseTimeMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing model {ModelId}", modelId);

                    results.Add(new ModelResult
                    {
                        ModelId = modelId,
                        Response = $"Unexpected error: {ex.Message}",
                        ResponseTimeMs = 0,
                        Status = "error",
                        ErrorMessage = ex.Message
                    });
                }
            }

            _logger.LogInformation("Sequential comparison completed. Processed {ProcessedCount}/{TotalCount} models",
                results.Count, modelIds.Count);

            return results;
        }

        private (string provider, string apiKey, string baseUrl) GetProviderInfo(string modelId)
        {
            _logger.LogInformation("GetProviderInfo called for model: {ModelId}", modelId);
            _logger.LogInformation("NanoGPT available models: {Models}", string.Join(", ", _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>()));
            _logger.LogInformation("OpenRouter available models: {Models}", string.Join(", ", _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>()));

            // Determine provider based on model ID patterns
            // Models with ":free" suffix are typically OpenRouter models
            if (modelId.Contains(":free"))
            {
                _logger.LogInformation("Model {ModelId} assigned to OpenRouter provider (free model)", modelId);
                return ("OpenRouter", _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
            }

            // Check if model is in NanoGPT's available models
            if (_apiConfiguration.NanoGPT?.AvailableModels?.Contains(modelId) == true)
            {
                _logger.LogInformation("Model {ModelId} assigned to NanoGPT provider", modelId);
                return ("NanoGPT", _apiConfiguration.NanoGPT.ApiKey, _apiConfiguration.NanoGPT.BaseUrl);
            }

            // Check if model is in OpenRouter's available models
            if (_apiConfiguration.OpenRouter?.AvailableModels?.Contains(modelId) == true)
            {
                _logger.LogInformation("Model {ModelId} assigned to OpenRouter provider", modelId);
                return ("OpenRouter", _apiConfiguration.OpenRouter.ApiKey, _apiConfiguration.OpenRouter.BaseUrl);
            }

            // If model is not found in either provider, log warning and default to OpenRouter
            _logger.LogWarning("Model {ModelId} not found in configured providers, defaulting to OpenRouter", modelId);
            return ("OpenRouter", _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
        }
    }

    /// <summary>
    /// Result from a single model analysis
    /// </summary>
    public class AnalysisResult
    {
        public string ModelId { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public long ResponseTimeMs { get; set; }
        public int? TokenCount { get; set; }
        public string Status { get; set; } = "success";
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result from a single model in comparison
    /// </summary>
    public class ModelResult
    {
        public string ModelId { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public long ResponseTimeMs { get; set; }
        public int? TokenCount { get; set; }
        public string Status { get; set; } = "success";
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// OpenRouter API response structure
    /// </summary>
    public class OpenRouterResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        public Usage Usage { get; set; } = new Usage();
    }

    public class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; } = new Message();
        public string FinishReason { get; set; } = string.Empty;
    }

    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
