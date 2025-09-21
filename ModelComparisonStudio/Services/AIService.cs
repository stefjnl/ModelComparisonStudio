using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ModelComparisonStudio.Services
{
    /// <summary>
    /// Service for interacting with AI models from multiple providers
    /// </summary>
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
            
            // Configure HttpClient for larger requests and longer timeouts
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Increased from default 100 seconds
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
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
                _logger.LogInformation("=== AnalyzeCodeAsync Started ===");
                _logger.LogInformation("Starting analysis with model {ModelId}", modelId);
                _logger.LogInformation("Prompt: {Prompt}", prompt);
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

                // Prepare the request using the exact NanoGPT API format that works
                var promptLength = prompt.Length;
                var maxTokens = Math.Min(4000, Math.Max(1000, promptLength / 2)); // Dynamic calculation
                if (promptLength > 2000) maxTokens = 4000; // For very large prompts, use max tokens
                
                // Use the exact format from the working curl command
                // Map the model ID to the correct NanoGPT API model name
                string nanoGptModelName = MapModelIdToNanoGptName(modelId);
                
                var request = new
                {
                    model = nanoGptModelName, // Use the mapped model name
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    stream = false,
                    temperature = 0.7,
                    max_tokens = maxTokens,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0,
                    cache_control = new
                    {
                        enabled = false
                    }
                };

                // Log request size for debugging
                _logger.LogInformation("Request details - Prompt length: {PromptLength} characters, Model: {ModelId}, Max tokens: {MaxTokens}",
                    promptLength, modelId, maxTokens);

                var jsonRequest = JsonSerializer.Serialize(request);
                _logger.LogInformation("Request JSON: {RequestJson}", jsonRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Set up headers
                _logger.LogInformation("Setting up HTTP headers for {Provider} API call", provider);
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
                // Add provider-specific headers
                if (provider == "OpenRouter")
                {
                    _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://modelcomparisonstudio.com");
                    _httpClient.DefaultRequestHeaders.Add("X-Title", "Model Comparison Studio");
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }
                else if (provider == "NanoGPT")
                {
                    // NanoGPT requires text/event-stream accept header
                    _httpClient.DefaultRequestHeaders.Accept.Clear();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                }
                
                _logger.LogInformation("HTTP Headers set - Authorization: Bearer [REDACTED], Accept: {AcceptHeader}",
                    _httpClient.DefaultRequestHeaders.Accept.ToString());

                // Make the API call with enhanced error handling for large requests
                _logger.LogInformation("Making API call to {BaseUrl}/chat/completions for model {ModelId}", baseUrl, modelId);
                
                try
                {
                    var response = await _httpClient.PostAsync($"{baseUrl}/chat/completions", content, cancellationToken);
                    
                    // Process response normally
                    stopwatch.Stop();
                    var apiResponseTime = stopwatch.ElapsedMilliseconds;

                    _logger.LogInformation("API call completed in {ResponseTime}ms with status code {StatusCode}", apiResponseTime, (int)response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError("API call failed for model {ModelId}: {StatusCode} - {ErrorContent}",
                            modelId, response.StatusCode, errorContent);

                        return new AnalysisResult
                        {
                            ModelId = modelId,
                            Response = $"Error: {response.StatusCode} - {response.ReasonPhrase}",
                            ResponseTimeMs = apiResponseTime,
                            Status = "error",
                            ErrorMessage = errorContent
                        };
                    }

                    // Process successful response
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogInformation("Raw API response: {ResponseJson}", responseJson);
                    
                    // Try to deserialize with more robust error handling
                    OpenRouterResponse? deserializedResponse = null;
                    try
                    {
                        _logger.LogInformation("Attempting JSON deserialization...");
                        deserializedResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        });
                        _logger.LogInformation("JSON deserialization completed successfully");
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON deserialization failed: {ErrorMessage}", jsonEx.Message);
                        _logger.LogError("JSON parsing error details - Path: {Path}, LineNumber: {LineNumber}, BytePositionInLine: {BytePositionInLine}",
                            jsonEx.Path, jsonEx.LineNumber, jsonEx.BytePositionInLine);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "General deserialization error: {ErrorMessage}", ex.Message);
                    }

                    _logger.LogInformation("Deserialized API response - ID: '{Id}', Object: '{Object}', Model: '{Model}', Choices: {ChoiceCount}, Provider: '{Provider}'",
                        deserializedResponse?.Id ?? "NULL",
                        deserializedResponse?.Object ?? "NULL",
                        deserializedResponse?.Model ?? "NULL",
                        deserializedResponse?.Choices?.Length ?? -1,
                        deserializedResponse?.Provider ?? "NULL");

                    // Also log the raw JSON structure for debugging
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseJson);
                        _logger.LogInformation("Raw JSON structure analysis - Root element: {RootElement}, Has choices property: {HasChoices}, Choices array length: {ChoicesLength}",
                            jsonDoc.RootElement.ValueKind,
                            jsonDoc.RootElement.TryGetProperty("choices", out var choicesProp),
                            choicesProp.ValueKind == JsonValueKind.Array ? choicesProp.GetArrayLength() : -1);
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Failed to parse JSON structure: {ErrorMessage}", parseEx.Message);
                    }

                    if (deserializedResponse?.Choices == null)
                    {
                        _logger.LogError("API response choices is null for model {ModelId}", modelId);
                        throw new InvalidOperationException("API response choices is null");
                    }

                    if (deserializedResponse.Choices.Length == 0)
                    {
                        _logger.LogError("No response choices returned from API for model {ModelId}. Response: {ResponseJson}", modelId, responseJson);
                        throw new InvalidOperationException($"No response choices returned from API. Raw response: {responseJson}");
                    }

                    _logger.LogInformation("Processing choice 0 - Role: {Role}, Content length: {ContentLength}, Finish reason: {FinishReason}",
                        deserializedResponse.Choices[0].Message?.Role ?? "NULL",
                        deserializedResponse.Choices[0].Message?.Content?.Length ?? 0,
                        deserializedResponse.Choices[0].FinishReason ?? "NULL");

                    var analysisResult = new AnalysisResult
                    {
                        ModelId = modelId,
                        Response = deserializedResponse.Choices[0].Message.Content,
                        ResponseTimeMs = apiResponseTime,
                        TokenCount = deserializedResponse.Usage?.TotalTokens,
                        Status = "success"
                    };

                    _logger.LogInformation("Analysis completed for model {ModelId} in {ResponseTime}ms with {TokenCount} tokens",
                        modelId, apiResponseTime, deserializedResponse.Usage?.TotalTokens ?? 0);

                    return analysisResult;
                }
                catch (TaskCanceledException tex) when (tex.InnerException is TimeoutException)
                {
                    stopwatch.Stop();
                    _logger.LogError(tex, "Request timeout for model {ModelId} after {ResponseTime}ms. Prompt length: {PromptLength}",
                        modelId, stopwatch.ElapsedMilliseconds, prompt.Length);
                    
                    return new AnalysisResult
                    {
                        ModelId = modelId,
                        Response = "Error: Request timeout - the model took too long to respond. Try a shorter prompt.",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Status = "error",
                        ErrorMessage = $"Request timeout after {stopwatch.ElapsedMilliseconds}ms. The prompt may be too long or the model may be overloaded."
                    };
                }
                catch (TaskCanceledException tex) when (cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(tex, "Request cancelled for model {ModelId} after {ResponseTime}ms",
                        modelId, stopwatch.ElapsedMilliseconds);
                    
                    return new AnalysisResult
                    {
                        ModelId = modelId,
                        Response = "Error: Request was cancelled.",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Status = "error",
                        ErrorMessage = "Request was cancelled by user."
                    };
                }
                catch (HttpRequestException hex)
                {
                    stopwatch.Stop();
                    _logger.LogError(hex, "HTTP request error for model {ModelId}: {ErrorMessage}. Prompt length: {PromptLength}",
                        modelId, hex.Message, prompt.Length);
                    
                    return new AnalysisResult
                    {
                        ModelId = modelId,
                        Response = $"Error: HTTP request failed - {hex.Message}",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Status = "error",
                        ErrorMessage = $"HTTP request error: {hex.Message}. This may be due to network issues or request size limits."
                    };
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error analyzing code with model {ModelId}. Error: {ErrorMessage}", modelId, ex.Message);

                return new AnalysisResult
                {
                    ModelId = modelId,
                    Response = $"Error: {ex.Message}",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Status = "error",
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                _logger.LogInformation("=== AnalyzeCodeAsync Completed ===");
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
            _logger.LogInformation("=== GetProviderInfo Debug Start ===");
            _logger.LogInformation("GetProviderInfo called for model: {ModelId}", modelId);
            
            var nanoGptModels = _apiConfiguration.NanoGPT?.AvailableModels ?? Array.Empty<string>();
            var openRouterModels = _apiConfiguration.OpenRouter?.AvailableModels ?? Array.Empty<string>();
            
            _logger.LogInformation("NanoGPT available models ({Count}): {Models}", nanoGptModels.Length, string.Join(", ", nanoGptModels));
            _logger.LogInformation("OpenRouter available models ({Count}): {Models}", openRouterModels.Length, string.Join(", ", openRouterModels));

            // First check if model is in NanoGPT's available models (case-insensitive)
            // This takes priority over any suffix patterns
            bool isInNanoGPT = nanoGptModels.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Model {ModelId} in NanoGPT models: {IsInNanoGPT}", modelId, isInNanoGPT);
            
            if (isInNanoGPT)
            {
                _logger.LogInformation("Model {ModelId} assigned to NanoGPT provider", modelId);
                return ("NanoGPT", _apiConfiguration.NanoGPT?.ApiKey ?? string.Empty, _apiConfiguration.NanoGPT?.BaseUrl ?? string.Empty);
            }

            // Determine provider based on model ID patterns
            // Models with ":free" suffix are typically OpenRouter models
            if (modelId.Contains(":free"))
            {
                _logger.LogInformation("Model {ModelId} assigned to OpenRouter provider (free model)", modelId);
                return ("OpenRouter", _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
            }

            // Check if model is in OpenRouter's available models (case-insensitive)
            bool isInOpenRouter = openRouterModels.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Model {ModelId} in OpenRouter models: {IsInOpenRouter}", modelId, isInOpenRouter);
            
            if (isInOpenRouter)
            {
                _logger.LogInformation("Model {ModelId} assigned to OpenRouter provider", modelId);
                return ("OpenRouter", _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
            }

            // If model is not found in either provider, log warning and default to OpenRouter
            _logger.LogWarning("Model {ModelId} not found in configured providers, defaulting to OpenRouter", modelId);
            _logger.LogInformation("=== GetProviderInfo Debug End ===");
            return ("OpenRouter", _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
        }

        /// <summary>
        /// Maps model ID to the correct NanoGPT API model name
        /// NanoGPT API expects the actual model ID from configuration, not generic names
        /// </summary>
        private string MapModelIdToNanoGptName(string modelId)
        {
            // For NanoGPT API, we need to use the exact model ID from configuration
            // Remove any suffixes that might cause issues with the API
            var baseModelId = modelId.Split(':')[0]; // Remove any suffix after colon
            
            // Special handling for models that need specific mapping
            if (baseModelId.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
                return "deepseek-chat"; // This works as confirmed by successful calls
            else if (baseModelId.Contains("gpt", StringComparison.OrdinalIgnoreCase))
                return "chatgpt-4o-latest"; // Map GPT models to the working model
            
            // For other models, try using the base model ID as-is
            // If this doesn't work, we may need to get the actual NanoGPT API model names
            return baseModelId;
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
        public string Provider { get; set; } = string.Empty;
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        public Usage Usage { get; set; } = new Usage();
        public string SystemFingerprint { get; set; } = string.Empty;
    }

    public class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; } = new Message();
        public string FinishReason { get; set; } = string.Empty;
        public string? NativeFinishReason { get; set; }
        public object? Logprobs { get; set; }
    }

    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Refusal { get; set; }
        public string? Reasoning { get; set; }
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public UsageDetails? PromptTokensDetails { get; set; }
        public UsageDetails? CompletionTokensDetails { get; set; }
    }

    public class UsageDetails
    {
        public int CachedTokens { get; set; }
        public int AudioTokens { get; set; }
        public int ReasoningTokens { get; set; }
    }
}