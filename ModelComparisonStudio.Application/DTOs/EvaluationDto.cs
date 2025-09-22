using System.ComponentModel.DataAnnotations;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Application.DTOs;

/// <summary>
/// Data Transfer Object for evaluation responses.
/// </summary>
public class EvaluationDto
{
    /// <summary>
    /// Unique identifier for this evaluation.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The prompt ID that was used for this comparison.
    /// </summary>
    [Required]
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// The original prompt text that was sent to the model.
    /// </summary>
    [Required]
    public string PromptText { get; set; } = string.Empty;

    /// <summary>
    /// The model ID that was evaluated.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The user's rating (1-10 stars).
    /// </summary>
    [Range(1, 10)]
    public int? Rating { get; set; }

    /// <summary>
    /// The user's comment about the model response.
    /// </summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// The response time in milliseconds from the model.
    /// </summary>
    public long? ResponseTimeMs { get; set; } // Nullable, defaults to 1000ms if not provided

    /// <summary>
    /// The number of tokens used in the response.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Timestamp when the evaluation was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the evaluation was last updated.
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the evaluation has been saved to persistent storage.
    /// </summary>
    public bool IsSaved { get; set; }

    /// <summary>
    /// Converts a domain evaluation to this DTO.
    /// </summary>
    /// <param name="evaluation">The domain evaluation object.</param>
    /// <returns>A new DTO instance.</returns>
    public static EvaluationDto FromDomainEvaluation(Evaluation evaluation)
    {
        return new EvaluationDto
        {
            Id = evaluation.Id.ToString(),
            PromptId = evaluation.PromptId,
            PromptText = evaluation.PromptText,
            ModelId = evaluation.ModelId,
            Rating = evaluation.Rating,
            Comment = evaluation.Comment.ToString(),
            ResponseTimeMs = evaluation.ResponseTimeMs,
            TokenCount = evaluation.TokenCount,
            CreatedAt = evaluation.CreatedAt,
            UpdatedAt = evaluation.UpdatedAt,
            IsSaved = evaluation.IsSaved
        };
    }

    /// <summary>
    /// Converts this DTO to a domain evaluation.
    /// </summary>
    /// <returns>A domain evaluation.</returns>
    public Evaluation ToDomainEvaluation()
    {
        var evaluation = Evaluation.Create(
            PromptId,
            PromptText,
            ModelId,
            ResponseTimeMs ?? 1000, // Default to 1 second if not provided
            TokenCount);

        if (Rating.HasValue)
        {
            evaluation.UpdateRating(Rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(Comment))
        {
            evaluation.UpdateComment(Comment);
        }

        return evaluation;
    }
}

/// <summary>
/// Data Transfer Object for creating evaluations.
/// </summary>
public class CreateEvaluationDto
{
    /// <summary>
    /// The prompt ID that was used for this comparison.
    /// </summary>
    [Required]
    public string PromptId { get; set; } = string.Empty;

    /// <summary>
    /// The original prompt text that was sent to the model.
    /// </summary>
    [Required]
    public string PromptText { get; set; } = string.Empty;

    /// <summary>
    /// The model ID that was evaluated.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The response time in milliseconds from the model.
    /// </summary>
    public long ResponseTimeMs { get; set; } = 1000; // Default to 1 second if not specified

    /// <summary>
    /// The number of tokens used in the response.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// The user's rating (1-10 stars).
    /// </summary>
    [Range(1, 10)]
    public int? Rating { get; set; }

    /// <summary>
    /// The user's comment about the model response.
    /// </summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for updating evaluation ratings.
/// </summary>
public class UpdateRatingDto
{
    /// <summary>
    /// The user's rating (1-10 stars).
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int Rating { get; set; }
}

/// <summary>
/// Data Transfer Object for updating evaluation comments.
/// </summary>
public class UpdateCommentDto
{
    /// <summary>
    /// The user's comment about the model response.
    /// </summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for evaluation statistics.
/// </summary>
public class EvaluationStatisticsDto
{
    /// <summary>
    /// The model ID.
    /// </summary>
    [Required]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The average rating for the model.
    /// </summary>
    public double? AverageRating { get; set; }

    /// <summary>
    /// The total number of evaluations for the model.
    /// </summary>
    [Required]
    public int TotalEvaluations { get; set; }

    /// <summary>
    /// The number of evaluations with ratings.
    /// </summary>
    [Required]
    public int RatedEvaluations { get; set; }

    /// <summary>
    /// The number of evaluations with comments.
    /// </summary>
    [Required]
    public int CommentedEvaluations { get; set; }

    /// <summary>
    /// The average response time in milliseconds.
    /// </summary>
    public double AverageSpeed { get; set; }

    /// <summary>
    /// The average token count.
    /// </summary>
    public double AverageTokens { get; set; }

    /// <summary>
    /// The percentage of evaluations with comments.
    /// </summary>
    public double CommentRate { get; set; }

    /// <summary>
    /// The number of days since the last evaluation.
    /// </summary>
    public int LastEvaluated { get; set; }

    /// <summary>
    /// The distribution of ratings (1-10) for this model.
    /// Index 0 represents 1-star, index 9 represents 10-star.
    /// </summary>
    [Required]
    public int[] RatingDistribution { get; set; } = new int[10];
}
