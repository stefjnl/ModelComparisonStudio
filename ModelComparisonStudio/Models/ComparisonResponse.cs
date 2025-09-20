using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Models
{
    /// <summary>
    /// Response model for comparison execution
    /// </summary>
    public class ComparisonResponse
    {
        /// <summary>
        /// Unique identifier for this comparison
        /// </summary>
        [Required]
        public string ComparisonId { get; set; } = string.Empty;

        /// <summary>
        /// The original prompt that was sent to all models
        /// </summary>
        [Required]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// List of results from all models
        /// </summary>
        [Required]
        public List<ModelResult> Results { get; set; } = new List<ModelResult>();

        /// <summary>
        /// Timestamp when the comparison was executed
        /// </summary>
        [Required]
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total number of models processed
        /// </summary>
        public int TotalModels => Results.Count;

        /// <summary>
        /// Number of successful model responses
        /// </summary>
        public int SuccessfulModels => Results.Count(r => r.Status == "success");

        /// <summary>
        /// Number of failed model responses
        /// </summary>
        public int FailedModels => Results.Count(r => r.Status == "error");

        /// <summary>
        /// Average response time across all models (in milliseconds)
        /// </summary>
        public double AverageResponseTime => Results.Any(r => r.Status == "success")
            ? Results.Where(r => r.Status == "success").Average(r => r.ResponseTimeMs)
            : 0;

        /// <summary>
        /// Total tokens used across all models
        /// </summary>
        public int TotalTokens => Results.Sum(r => r.TokenCount ?? 0);
    }

    /// <summary>
    /// Result from a single model in comparison
    /// </summary>
    public class ModelResult
    {
        /// <summary>
        /// The model ID that was used
        /// </summary>
        [Required]
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// The response from the model
        /// </summary>
        [Required]
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        [Required]
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Number of tokens used (optional, may be null if not available)
        /// </summary>
        public int? TokenCount { get; set; }

        /// <summary>
        /// Status of the model execution ("success", "error", "timeout")
        /// </summary>
        [Required]
        public string Status { get; set; } = "success";

        /// <summary>
        /// Error message if the model failed (optional)
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Provider name (NanoGPT or OpenRouter)
        /// </summary>
        public string Provider { get; set; } = string.Empty;
    }
}
