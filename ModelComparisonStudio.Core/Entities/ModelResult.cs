using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Represents the result from a single model in a comparison.
/// </summary>
public class ModelResult
{
    /// <summary>
    /// The model ID that was used.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The response from the model.
    /// </summary>
    [Required]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    [Required]
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Number of tokens used (optional, may be null if not available).
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Status of the model execution ("success", "error", "timeout").
    /// </summary>
    [Required]
    public string Status { get; set; } = "success";

    /// <summary>
    /// Error message if the model failed (optional).
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Provider name (NanoGPT or OpenRouter).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private ModelResult() { }

    /// <summary>
    /// Creates a model result with specified status and error message.
    /// </summary>
    /// <param name="modelId">The model ID that was used.</param>
    /// <param name="response">The response from the model.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    /// <param name="tokenCount">Number of tokens used (optional).</param>
    /// <param name="status">Status of the model execution ("success", "error", "timeout").</param>
    /// <param name="errorMessage">Error message if the model failed (optional).</param>
    /// <param name="provider">Provider name.</param>
    /// <returns>A new model result.</returns>
    public static ModelResult Create(
        string modelId,
        string response,
        long responseTimeMs,
        int? tokenCount = null,
        string status = "success",
        string errorMessage = "",
        string provider = "")
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new ArgumentException("Response cannot be null or empty.", nameof(response));
        }

        if (responseTimeMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeMs), "Response time must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status cannot be null or empty.", nameof(status));
        }

        return new ModelResult
        {
            ModelId = modelId,
            Response = response,
            ResponseTimeMs = responseTimeMs,
            TokenCount = tokenCount,
            Status = status,
            ErrorMessage = errorMessage,
            Provider = provider
        };
    }

    /// <summary>
    /// Creates a successful model result.
    /// </summary>
    /// <param name="modelId">The model ID that was used.</param>
    /// <param name="response">The response from the model.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    /// <param name="tokenCount">Number of tokens used (optional).</param>
    /// <param name="provider">Provider name.</param>
    /// <returns>A new successful model result.</returns>
    public static ModelResult CreateSuccess(
        string modelId,
        string response,
        long responseTimeMs,
        int? tokenCount = null,
        string provider = "")
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new ArgumentException("Response cannot be null or empty.", nameof(response));
        }

        if (responseTimeMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeMs), "Response time must be non-negative.");
        }

        return new ModelResult
        {
            ModelId = modelId,
            Response = response,
            ResponseTimeMs = responseTimeMs,
            TokenCount = tokenCount,
            Status = "success",
            Provider = provider
        };
    }

    /// <summary>
    /// Creates an error model result.
    /// </summary>
    /// <param name="modelId">The model ID that was used.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    /// <param name="provider">Provider name.</param>
    /// <returns>A new error model result.</returns>
    public static ModelResult CreateError(
        string modelId,
        string errorMessage,
        long responseTimeMs,
        string provider = "")
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
        }

        if (responseTimeMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeMs), "Response time must be non-negative.");
        }

        return new ModelResult
        {
            ModelId = modelId,
            Response = $"Error: {errorMessage}",
            ResponseTimeMs = responseTimeMs,
            Status = "error",
            ErrorMessage = errorMessage,
            Provider = provider
        };
    }

    /// <summary>
    /// Creates a timeout model result.
    /// </summary>
    /// <param name="modelId">The model ID that was used.</param>
    /// <param name="timeoutMs">Timeout duration in milliseconds.</param>
    /// <param name="provider">Provider name.</param>
    /// <returns>A new timeout model result.</returns>
    public static ModelResult CreateTimeout(
        string modelId,
        long timeoutMs,
        string provider = "")
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        if (timeoutMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be non-negative.");
        }

        return new ModelResult
        {
            ModelId = modelId,
            Response = "Error: Request timeout - the model took too long to respond.",
            ResponseTimeMs = timeoutMs,
            Status = "timeout",
            ErrorMessage = $"Request timeout after {timeoutMs}ms",
            Provider = provider
        };
    }

    /// <summary>
    /// Determines if this result was successful.
    /// </summary>
    /// <returns>True if the status is "success", false otherwise.</returns>
    public bool IsSuccessful()
    {
        return Status == "success";
    }

    /// <summary>
    /// Determines if this result represents an error.
    /// </summary>
    /// <returns>True if the status is "error", false otherwise.</returns>
    public bool IsError()
    {
        return Status == "error";
    }

    /// <summary>
    /// Determines if this result represents a timeout.
    /// </summary>
    /// <returns>True if the status is "timeout", false otherwise.</returns>
    public bool IsTimeout()
    {
        return Status == "timeout";
    }

    /// <summary>
    /// Gets the response length in characters.
    /// </summary>
    /// <returns>The length of the response string.</returns>
    public int GetResponseLength()
    {
        return Response.Length;
    }

    /// <summary>
    /// Gets the response time in seconds.
    /// </summary>
    /// <returns>The response time in seconds.</returns>
    public double GetResponseTimeSeconds()
    {
        return ResponseTimeMs / 1000.0;
    }
}
