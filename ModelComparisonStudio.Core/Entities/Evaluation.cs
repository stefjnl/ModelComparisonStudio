using System.ComponentModel.DataAnnotations;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Represents a user evaluation of a model response in the domain.
/// </summary>
public class Evaluation
{
    /// <summary>
    /// Unique identifier for this evaluation.
    /// </summary>
    [Required]
    public EvaluationId Id { get; private set; }

    /// <summary>
    /// The prompt ID that was used for this comparison.
    /// </summary>
    [Required]
    public string PromptId { get; private set; } = string.Empty;

    /// <summary>
    /// The original prompt text that was sent to the model.
    /// </summary>
    [Required]
    public string PromptText { get; private set; } = string.Empty;

    /// <summary>
    /// The model ID that was evaluated.
    /// </summary>
    [Required]
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>
    /// The user's rating (1-10 stars).
    /// </summary>
    [Range(1, 10)]
    public int? Rating { get; private set; }

    /// <summary>
    /// The user's comment about the model response.
    /// </summary>
    [MaxLength(500)]
    public CommentText Comment { get; private set; }

    /// <summary>
    /// The response time in milliseconds from the model.
    /// </summary>
    public long ResponseTimeMs { get; private set; } = 1000; // Default to 1 second if not specified

    /// <summary>
    /// The number of tokens used in the response.
    /// </summary>
    public int? TokenCount { get; private set; }

    /// <summary>
    /// Timestamp when the evaluation was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the evaluation was last updated.
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Indicates whether the evaluation has been saved to persistent storage.
    /// </summary>
    public bool IsSaved { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private Evaluation() { }

    /// <summary>
    /// Creates a new evaluation with the specified parameters.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="promptText">The prompt text.</param>
    /// <param name="modelId">The model ID.</param>
    /// <param name="responseTimeMs">The response time in milliseconds (optional, defaults to 1000ms).</param>
    /// <param name="tokenCount">The token count (optional).</param>
    /// <returns>A new evaluation instance.</returns>
    public static Evaluation Create(
        string promptId,
        string promptText,
        string modelId,
        long responseTimeMs = 1000,
        int? tokenCount = null)
    {
        if (string.IsNullOrWhiteSpace(promptId))
            throw new ArgumentException("Prompt ID cannot be null or empty.", nameof(promptId));
        
        if (string.IsNullOrWhiteSpace(promptText))
            throw new ArgumentException("Prompt text cannot be null or empty.", nameof(promptText));
        
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        

        return new Evaluation
        {
            Id = EvaluationId.Create(),
            PromptId = promptId,
            PromptText = promptText,
            ModelId = modelId,
            ResponseTimeMs = responseTimeMs, // Will use default value of 1000 if not provided
            TokenCount = tokenCount,
            Comment = CommentText.CreateEmpty(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsSaved = false
        };
    }

    /// <summary>
    /// Updates the rating for this evaluation.
    /// </summary>
    /// <param name="rating">The new rating (1-10).</param>
    public void UpdateRating(int rating)
    {
        if (rating < 1 || rating > 10)
            throw new ArgumentException("Rating must be between 1 and 10.", nameof(rating));

        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
        IsSaved = false;
    }

    /// <summary>
    /// Updates the comment for this evaluation.
    /// </summary>
    /// <param name="comment">The new comment text.</param>
    public void UpdateComment(string comment)
    {
        Comment = CommentText.Create(comment);
        UpdatedAt = DateTime.UtcNow;
        IsSaved = false;
    }

    /// <summary>
    /// Updates the response time and token count for this evaluation.
    /// </summary>
    /// <param name="responseTimeMs">The new response time in milliseconds.</param>
    /// <param name="tokenCount">The new token count.</param>
    public void UpdateResponseTimeAndTokenCount(long responseTimeMs, int? tokenCount = null)
    {
        ResponseTimeMs = responseTimeMs;
        TokenCount = tokenCount;
        UpdatedAt = DateTime.UtcNow;
        IsSaved = false;
    }

    /// <summary>
    /// Marks the evaluation as saved to persistent storage.
    /// </summary>
    public void MarkAsSaved()
    {
        IsSaved = true;
    }

    /// <summary>
    /// Checks if the evaluation has unsaved changes.
    /// </summary>
    /// <returns>True if there are unsaved changes, false otherwise.</returns>
    public bool HasUnsavedChanges()
    {
        return !IsSaved;
    }

    /// <summary>
    /// Validates the evaluation for persistence.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(PromptId))
            errors.Add("Prompt ID is required");

        if (string.IsNullOrWhiteSpace(PromptText))
            errors.Add("Prompt text is required");

        if (string.IsNullOrWhiteSpace(ModelId))
            errors.Add("Model ID is required");


        if (Rating.HasValue && (Rating < 1 || Rating > 10))
            errors.Add("Rating must be between 1 and 10");

        return errors;
    }
}