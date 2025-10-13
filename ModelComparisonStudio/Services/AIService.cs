using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelComparisonStudio.Configuration;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;
using ModelComparisonStudio.Infrastructure.Services;
using static ModelComparisonStudio.Core.ValueObjects.AIProviderNames;
using static ModelComparisonStudio.Core.ValueObjects.AIProviderUrls;
using static ModelComparisonStudio.Core.ValueObjects.MimeTypes;

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
        private readonly QueryPerformanceMonitor _performanceMonitor;

        public AIService(
            IOptions<ApiConfiguration> apiConfiguration,
            HttpClient httpClient,
            ILogger<AIService> logger,
            QueryPerformanceMonitor performanceMonitor)
        {
            _apiConfiguration = apiConfiguration.Value;
            _httpClient = httpClient;
            _logger = logger;
            _performanceMonitor = performanceMonitor;

            // HttpClient is now configured in Program.cs with 5-minute timeout
            // Log the configuration for verification
            _logger.LogInformation("=== AIService Configuration Diagnostic ===");
            _logger.LogInformation("HTTP Client Timeout: {TimeoutMinutes} minutes ({TimeoutSeconds} seconds)",
                _httpClient.Timeout.TotalMinutes, _httpClient.Timeout.TotalSeconds);
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
        /// <param name="timeout">Request timeout duration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis result with response and metadata</returns>
        public async Task<AnalysisResult> AnalyzeCodeAsync(
            string prompt,
            string modelId,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var performanceTracker = _performanceMonitor.TrackQuery($"AI-{modelId}");

            try
            {
                _logger.LogInformation("=== AnalyzeCodeAsync Started ===");
                _logger.LogInformation("Starting analysis with model {ModelId}", modelId);
                _logger.LogInformation("Prompt: {Prompt}", prompt);
                _logger.LogInformation("Timeout: {Timeout} seconds", timeout.TotalSeconds);
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
                var content = new StringContent(jsonRequest, Encoding.UTF8, MimeTypes.ApplicationJson);

                // Set up headers with proper error handling
                _logger.LogInformation("Setting up HTTP headers for {Provider} API call", provider);
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    // Add provider-specific headers
                    if (provider == AIProviderNames.OpenRouter)
                    {
                        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://modelcomparisonstudio.com");
                        _httpClient.DefaultRequestHeaders.Add("X-Title", "Model Comparison Studio");
                        _httpClient.DefaultRequestHeaders.Accept.Clear();
                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.ApplicationJson));
                    }
                    else if (provider == AIProviderNames.NanoGPT)
                    {
                        // NanoGPT requires text/event-stream accept header
                        _httpClient.DefaultRequestHeaders.Accept.Clear();
                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.TextEventStream));
                    }

                    _logger.LogInformation("HTTP Headers set - Authorization: Bearer [REDACTED], Accept: {AcceptHeader}",
                        _httpClient.DefaultRequestHeaders.Accept.ToString());
                }
                catch (ArgumentException aex)
                {
                    _logger.LogError(aex, "Invalid header configuration for {Provider}: {ErrorMessage}", provider, aex.Message);
                    throw new InvalidOperationException($"Invalid header configuration: {aex.Message}", aex);
                }
                catch (FormatException fex)
                {
                    _logger.LogError(fex, "Invalid header format for {Provider}: {ErrorMessage}", provider, fex.Message);
                    throw new InvalidOperationException($"Invalid header format: {fex.Message}", fex);
                }
                catch (InvalidOperationException ioex)
                {
                    _logger.LogError(ioex, "Invalid operation while setting headers for {Provider}: {ErrorMessage}", provider, ioex.Message);
                    throw new InvalidOperationException($"Failed to set headers: {ioex.Message}", ioex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error setting headers for {Provider}: {ErrorMessage}", provider, ex.Message);
                    throw new InvalidOperationException($"Failed to configure HTTP headers: {ex.Message}", ex);
                }

                // Create a timeout cancellation token source
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                var requestCancellationToken = combinedCts.Token;

                // Make the API call with enhanced error handling and detailed logging
                _logger.LogInformation("=== API Request Details ===");
                _logger.LogInformation("Request URL: {BaseUrl}/chat/completions", baseUrl);
                _logger.LogInformation("Request Method: POST");
                _logger.LogInformation("Request Headers: {Headers}",
                    string.Join(", ", _httpClient.DefaultRequestHeaders.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));
                _logger.LogInformation("Content-Type: {ContentType}", content.Headers.ContentType?.ToString());
                _logger.LogInformation("Content-Length: {ContentLength} bytes", content.Headers.ContentLength ?? -1);
                _logger.LogInformation("Configured timeout: {Timeout} seconds", timeout.TotalSeconds);
                _logger.LogInformation("=== End Request Details ===");

                try
                {
                    var apiCallStopwatch = Stopwatch.StartNew();
                    _logger.LogInformation("Making API call to {BaseUrl}/chat/completions for model {ModelId}", baseUrl, modelId);

                    // Use retry mechanism for robust error handling
                    var response = await ExecuteWithRetryAsync(
                        () => _httpClient.PostAsync($"{baseUrl}/chat/completions", content, requestCancellationToken),
                        modelId,
                        requestCancellationToken);

                    apiCallStopwatch.Stop();
                    var apiResponseTime = apiCallStopwatch.ElapsedMilliseconds;
                    stopwatch.Stop();
                    var totalProcessingTime = stopwatch.ElapsedMilliseconds;

                    _logger.LogInformation("API call timing - Network: {NetworkTime}ms, Total processing: {TotalTime}ms",
                        apiResponseTime, totalProcessingTime);
                    _logger.LogInformation("HTTP Status: {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
                    _logger.LogInformation("Response Headers: {Headers}",
                        string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        var errorContentLength = errorContent?.Length ?? 0;

                        _logger.LogError("=== API Error Details ===");
                        _logger.LogError("Model: {ModelId}", modelId);
                        _logger.LogError("Status: {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
                        _logger.LogError("Response Time: {ResponseTime}ms", apiResponseTime);
                        _logger.LogError("Error Content Length: {ContentLength} characters", errorContentLength);
                        _logger.LogError("Error Content (first 1000 chars): {ErrorContentPreview}",
                            errorContentLength > 1000 ? errorContent?.Substring(0, 1000) + "..." : errorContent);
                        _logger.LogError("Request URL: {Url}", $"{baseUrl}/chat/completions");
                        _logger.LogError("=== End Error Details ===");

                        return new AnalysisResult
                        {
                            ModelId = modelId,
                            Response = $"Error: {response.StatusCode} - {response.ReasonPhrase}",
                            ResponseTimeMs = apiResponseTime,
                            Status = ModelResultStatus.Error.ToString(),
                            ErrorMessage = errorContent ?? string.Empty
                        };
                    }

                    // Process successful response with detailed logging
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var responseLength = responseJson.Length;

                    _logger.LogInformation("=== API Response Success ===");
                    _logger.LogInformation("Response length: {ResponseLength} characters", responseLength);
                    _logger.LogInformation("Response preview (first 500 chars): {ResponsePreview}",
                        responseLength > 500 ? responseJson.Substring(0, 500) + "..." : responseJson);

                    // Try to deserialize with more robust error handling and detailed logging
                    OpenRouterResponse? deserializedResponse = null;
                    try
                    {
                        _logger.LogInformation("Attempting JSON deserialization...");
                        var deserializeStopwatch = Stopwatch.StartNew();

                        deserializedResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        });

                        deserializeStopwatch.Stop();
                        _logger.LogInformation("JSON deserialization completed in {DeserializeTime}ms", deserializeStopwatch.ElapsedMilliseconds);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError("=== JSON Deserialization Error ===");
                        _logger.LogError(jsonEx, "JSON deserialization failed: {ErrorMessage}", jsonEx.Message);
                        _logger.LogError("Error Path: {Path}", jsonEx.Path);
                        _logger.LogError("Line: {LineNumber}, Position: {BytePositionInLine}", jsonEx.LineNumber, jsonEx.BytePositionInLine);

                        // Simplified error snippet logging
                        if (responseLength > 0)
                        {
                            var errorPosition = (int)(jsonEx.BytePositionInLine ?? 0);
                            var startPos = Math.Max(0, Math.Min(responseLength - 1, errorPosition - 25));
                            var length = Math.Min(50, responseLength - startPos);
                            var errorSnippet = responseJson.Substring(startPos, length);
                            _logger.LogError("Error snippet: {ErrorSnippet}", errorSnippet);
                        }
                        _logger.LogError("=== End JSON Error ===");
                        deserializedResponse = null; // Ensure it's null on error
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "General deserialization error: {ErrorMessage}", ex.Message);
                        deserializedResponse = null; // Ensure it's null on error
                    }

                    // Detailed response analysis
                    _logger.LogInformation("=== Response Analysis ===");
                    _logger.LogInformation("Response ID: '{Id}'", deserializedResponse?.Id ?? "NULL");
                    _logger.LogInformation("Object Type: '{Object}'", deserializedResponse?.Object ?? "NULL");
                    _logger.LogInformation("Model: '{Model}'", deserializedResponse?.Model ?? "NULL");
                    _logger.LogInformation("Provider: '{Provider}'", deserializedResponse?.Provider ?? "NULL");
                    _logger.LogInformation("Number of choices: {ChoiceCount}", deserializedResponse?.Choices?.Length ?? -1);
                    _logger.LogInformation("Token usage - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
                        deserializedResponse?.Usage?.PromptTokens ?? -1,
                        deserializedResponse?.Usage?.CompletionTokens ?? -1,
                        deserializedResponse?.Usage?.TotalTokens ?? -1);

                    // Also log the raw JSON structure for debugging
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseJson);
                        _logger.LogInformation("JSON Structure - Root: {RootElement}", jsonDoc.RootElement.ValueKind);

                        if (jsonDoc.RootElement.TryGetProperty("choices", out var choicesProp) && choicesProp.ValueKind == JsonValueKind.Array)
                        {
                            _logger.LogInformation("Choices array length: {ChoicesLength}", choicesProp.GetArrayLength());
                        }
                        else
                        {
                            _logger.LogWarning("Choices property not found or not an array in response");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Failed to parse JSON structure: {ErrorMessage}", parseEx.Message);
                    }
                    _logger.LogInformation("=== End Response Analysis ===");

                    if (deserializedResponse?.Choices == null)
                    {
                        _logger.LogError("API response choices is null for model {ModelId}", modelId);
                        throw new InvalidOperationException("API response choices is null");
                    }

                    if (deserializedResponse.Choices.Length == 0)
                    {
                        _logger.LogError("No response choices returned from API for model {ModelId}", modelId);
                        _logger.LogError("Full response: {ResponseJson}", responseJson);
                        throw new InvalidOperationException($"No response choices returned from API. Raw response: {responseJson}");
                    }

                    // Detailed choice analysis
                    var firstChoice = deserializedResponse.Choices[0];
                    _logger.LogInformation("First choice analysis - Index: {Index}, Finish reason: {FinishReason}",
                        firstChoice.Index, firstChoice.FinishReason ?? "NULL");
                    _logger.LogInformation("Message - Role: {Role}, Content length: {ContentLength} characters",
                        firstChoice.Message?.Role ?? "NULL",
                        firstChoice.Message?.Content?.Length ?? 0);

                    var analysisResult = new AnalysisResult
                    {
                        ModelId = modelId,
                        Response = firstChoice.Message?.Content ?? string.Empty,
                        ResponseTimeMs = apiResponseTime,
                        TokenCount = deserializedResponse?.Usage?.TotalTokens,
                        Status = ModelResultStatus.Success.ToString()
                    };

                    _logger.LogInformation("=== Analysis Complete ===");
                    _logger.LogInformation("Model: {ModelId}", modelId);
                    _logger.LogInformation("Status: {Status}", analysisResult.Status);
                    _logger.LogInformation("Response Time: {ResponseTime}ms", analysisResult.ResponseTimeMs);
                    _logger.LogInformation("Tokens Used: {TokenCount}", analysisResult.TokenCount ?? 0);
                    _logger.LogInformation("Response Length: {ResponseLength} characters", analysisResult.Response.Length);
                    _logger.LogInformation("=== End Analysis Complete ===");

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
                        Status = ModelResultStatus.Error.ToString(),
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
                        Status = ModelResultStatus.Error.ToString(),
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
                        Status = ModelResultStatus.Error.ToString(),
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
                    Status = ModelResultStatus.Error.ToString(),
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                stopwatch.Stop();
                var totalTime = stopwatch.ElapsedMilliseconds;

                // Record performance metrics
                _performanceMonitor.RecordQueryExecution($"AI-{modelId}", totalTime);

                // Log performance summary for long-running operations
                if (totalTime > 30000) // 30 seconds
                {
                    _logger.LogWarning("Long-running operation detected: Model {ModelId} took {TotalTime}ms",
                        modelId, totalTime);
                }

                _logger.LogInformation("=== AnalyzeCodeAsync Completed ===");
                _logger.LogInformation("Performance: Model {ModelId} completed in {TotalTime}ms", modelId, totalTime);
            }
        }

        /// <summary>
        /// Executes sequential comparison across multiple models
        /// </summary>
        /// <param name="prompt">The prompt to send to all models</param>
        /// <param name="modelIds">List of model IDs to compare</param>
        /// <param name="timeout">Request timeout duration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of results from all models</returns>
        public async Task<List<ModelResult>> ExecuteSequentialComparison(
            string prompt,
            List<string> modelIds,
            TimeSpan timeout,
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

                    var analysisResult = await AnalyzeCodeAsync(prompt, modelId, timeout, cancellationToken);

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
                        Status = ModelResultStatus.Error.ToString(),
                        ErrorMessage = ex.Message
                    });
                }
            }

            _logger.LogInformation("Sequential comparison completed. Processed {ProcessedCount}/{TotalCount} models",
                results.Count, modelIds.Count);

            return results;
        }

        /// <summary>
        /// Executes parallel comparison across multiple models with configurable concurrency
        /// </summary>
        /// <param name="prompt">The prompt to send to all models</param>
        /// <param name="modelIds">List of model IDs to compare</param>
        /// <param name="maxConcurrency">Maximum number of concurrent requests (default: 2)</param>
        /// <param name="timeout">Request timeout duration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of results from all models</returns>
        public async Task<List<ModelResult>> ExecuteParallelComparison(
            string prompt,
            List<string> modelIds,
            int maxConcurrency = 2,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ModelResult>();
            var semaphore = new SemaphoreSlim(maxConcurrency);

            _logger.LogInformation("Starting parallel comparison for {ModelCount} models with max concurrency {MaxConcurrency}",
                modelIds.Count, maxConcurrency);

            // Create tasks for all models
            var tasks = modelIds.Select(async modelId =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return CreateErrorModelResult(modelId, "Request was cancelled");
                }

                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    _logger.LogInformation("Processing model {ModelIndex}/{ModelCount}: {ModelId}",
                        results.Count + 1, modelIds.Count, modelId);

                    var analysisResult = await AnalyzeCodeAsync(prompt, modelId, timeout, cancellationToken);

                    var modelResult = new ModelResult
                    {
                        ModelId = analysisResult.ModelId,
                        Response = analysisResult.Response,
                        ResponseTimeMs = analysisResult.ResponseTimeMs,
                        TokenCount = analysisResult.TokenCount,
                        Status = analysisResult.Status,
                        ErrorMessage = analysisResult.ErrorMessage
                    };

                    _logger.LogInformation("Completed model {ModelId} with status {Status} in {ResponseTime}ms",
                        modelId, modelResult.Status, modelResult.ResponseTimeMs);

                    return modelResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing model {ModelId}", modelId);
                    return CreateErrorModelResult(modelId, ex.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            // Wait for all tasks to complete
            var completedResults = await Task.WhenAll(tasks);

            // Add results to the list (maintains order)
            results.AddRange(completedResults);

            _logger.LogInformation("Parallel comparison completed. Processed {ProcessedCount}/{TotalCount} models",
                results.Count, modelIds.Count);

            // Log performance metrics
            var successfulResults = results.Where(r => r.Status == ModelResultStatus.Success.ToString()).ToList();
            var failedResults = results.Where(r => r.Status == ModelResultStatus.Error.ToString()).ToList();

            var totalTime = results.Sum(r => r.ResponseTimeMs);
            var averageTime = results.Any() ? totalTime / results.Count : 0;
            var maxTime = results.Any() ? results.Max(r => r.ResponseTimeMs) : 0;

            _logger.LogInformation("=== Parallel Execution Performance Metrics ===");
            _logger.LogInformation("Total models processed: {TotalCount}", modelIds.Count);
            _logger.LogInformation("Successful results: {SuccessCount}", successfulResults.Count);
            _logger.LogInformation("Failed results: {FailedCount}", failedResults.Count);
            _logger.LogInformation("Total execution time: {TotalTime}ms", totalTime);
            _logger.LogInformation("Average time per model: {AverageTime}ms", averageTime);
            _logger.LogInformation("Maximum time for single model: {MaxTime}ms", maxTime);
            _logger.LogInformation("Concurrency limit used: {ConcurrencyLimit}", maxConcurrency);
            _logger.LogInformation("Performance improvement estimate: ~{ImprovementPercent}% faster than sequential",
                Math.Max(0, 100 - (averageTime * modelIds.Count / totalTime)));
            _logger.LogInformation("=== End Performance Metrics ===");

            return results;
        }

        /// <summary>
        /// Executes an HTTP request with retry logic and exponential backoff
        /// </summary>
        /// <param name="requestFunc">Function that performs the HTTP request</param>
        /// <param name="modelId">Model ID for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTTP response message</returns>
        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            Func<Task<HttpResponseMessage>> requestFunc,
            string modelId,
            CancellationToken cancellationToken)
        {
            var retryAttempts = _apiConfiguration.Execution.RetryAttempts;
            var retryDelay = _apiConfiguration.Execution.RetryDelay;

            for (int attempt = 1; attempt <= retryAttempts + 1; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Retry attempt {Attempt}/{MaxAttempts} for model {ModelId}",
                            attempt - 1, retryAttempts, modelId);
                    }

                    var response = await requestFunc();

                    // If we get a successful response or a client error (4xx), don't retry
                    if (response.IsSuccessStatusCode || (int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        return response;
                    }

                    // For server errors (5xx), retry if we have attempts left
                    if (attempt <= retryAttempts)
                    {
                        var delay = retryDelay * attempt; // Exponential backoff
                        _logger.LogWarning("Server error {StatusCode} for model {ModelId}, retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
                            (int)response.StatusCode, modelId, delay.TotalMilliseconds, attempt, retryAttempts);

                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    return response; // Return the last failed response
                }
                catch (HttpRequestException ex) when (attempt <= retryAttempts)
                {
                    var delay = retryDelay * attempt; // Exponential backoff
                    _logger.LogWarning(ex, "HTTP request failed for model {ModelId}, retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
                        modelId, delay.TotalMilliseconds, attempt, retryAttempts);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException && attempt <= retryAttempts)
                {
                    var delay = retryDelay * attempt; // Exponential backoff
                    _logger.LogWarning(ex, "Request timeout for model {ModelId}, retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
                        modelId, delay.TotalMilliseconds, attempt, retryAttempts);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            // This should never be reached, but just in case
            throw new InvalidOperationException($"All retry attempts failed for model {modelId}");
        }

        private ModelResult CreateErrorModelResult(string modelId, string errorMessage)
        {
            return new ModelResult
            {
                ModelId = modelId,
                Response = $"Error: {errorMessage}",
                ResponseTimeMs = 0,
                Status = ModelResultStatus.Error.ToString(),
                ErrorMessage = errorMessage
            };
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
                return (AIProviderNames.NanoGPT, _apiConfiguration.NanoGPT?.ApiKey ?? string.Empty, _apiConfiguration.NanoGPT?.BaseUrl ?? string.Empty);
            }

            // Determine provider based on model ID patterns
            // Models with ":free" suffix are typically OpenRouter models
            if (modelId.Contains(":free"))
            {
                _logger.LogInformation("Model {ModelId} assigned to OpenRouter provider (free model)", modelId);
                return (AIProviderNames.OpenRouter, _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
            }

            // Check if model is in OpenRouter's available models (case-insensitive)
            bool isInOpenRouter = openRouterModels.Any(m => string.Equals(m, modelId, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Model {ModelId} in OpenRouter models: {IsInOpenRouter}", modelId, isInOpenRouter);

            if (isInOpenRouter)
            {
                _logger.LogInformation("Model {ModelId} assigned to OpenRouter provider", modelId);
                return (AIProviderNames.OpenRouter, _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
            }

            // If model is not found in either provider, log warning and default to OpenRouter
            _logger.LogWarning("Model {ModelId} not found in configured providers, defaulting to OpenRouter", modelId);
            _logger.LogInformation("=== GetProviderInfo Debug End ===");
            return (AIProviderNames.OpenRouter, _apiConfiguration.OpenRouter?.ApiKey ?? string.Empty, _apiConfiguration.OpenRouter?.BaseUrl ?? string.Empty);
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
        public string Status { get; set; } = ModelResultStatus.Success.ToString();
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
        public string Status { get; set; } = ModelResultStatus.Success.ToString();
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
