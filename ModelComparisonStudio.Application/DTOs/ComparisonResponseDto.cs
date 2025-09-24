using System.ComponentModel.DataAnnotations;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Application.DTOs;

/// <summary>
/// Data Transfer Object for comparison responses.
/// </summary>
public class ComparisonResponseDto
{
    /// <summary>
    /// Unique identifier for this comparison.
    /// </summary>
    [Required]
    public string ComparisonId { get; set; } = string.Empty;

    /// <summary>
    /// The original prompt that was sent to all models.
    /// </summary>
    [Required]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// List of results from all models.
    /// </summary>
    [Required]
    public List<ModelResultDto> Results { get; set; } = new();

    /// <summary>
    /// Timestamp when the comparison was executed.
    /// </summary>
    [Required]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of models processed.
    /// </summary>
    public int TotalModels => Results.Count;

    /// <summary>
    /// Number of successful model responses.
    /// </summary>
    public int SuccessfulModels => Results.Count(r => r.Status == ModelResultStatus.Success.ToString());

    /// <summary>
    /// Number of failed model responses.
    /// </summary>
    public int FailedModels => Results.Count(r => r.Status == ModelResultStatus.Error.ToString());

    /// <summary>
    /// Average response time across all models (in milliseconds).
    /// </summary>
    public double AverageResponseTime => Results.Any(r => r.Status == ModelResultStatus.Success.ToString())
        ? Results.Where(r => r.Status == ModelResultStatus.Success.ToString()).Average(r => r.ResponseTimeMs)
        : 0;

    /// <summary>
    /// Total tokens used across all models.
    /// </summary>
    public int TotalTokens => Results.Sum(r => r.TokenCount ?? 0);

    /// <summary>
    /// Converts a domain comparison to this DTO.
    /// </summary>
    /// <param name="comparison">The domain comparison object.</param>
    /// <returns>A new DTO instance.</returns>
    public static ComparisonResponseDto FromDomainComparison(Comparison comparison)
    {
        return new ComparisonResponseDto
        {
            ComparisonId = comparison.Id,
            Prompt = comparison.Prompt,
            Results = comparison.Results
                .Select(ModelResultDto.FromDomainModel)
                .ToList(),
            ExecutedAt = comparison.ExecutedAt
        };
    }

    /// <summary>
    /// Converts this DTO to a domain comparison response.
    /// </summary>
    /// <returns>A domain comparison response.</returns>
    public Core.Interfaces.ComparisonResponse ToDomainResponse()
    {
        var domainPrompt = Core.ValueObjects.Prompt.Create(Prompt);
        var domainResults = Results
            .Select(dto => dto.ToDomainModel())
            .ToList();

        return new Core.Interfaces.ComparisonResponse
        {
            ComparisonId = ComparisonId,
            Prompt = domainPrompt,
            Results = domainResults,
            ExecutedAt = ExecutedAt
        };
    }
}

/// <summary>
/// Data Transfer Object for individual model results.
/// </summary>
public class ModelResultDto
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
    /// Status of the model execution.
    /// </summary>
    [Required]
    public string Status { get; set; } = ModelResultStatus.Success.ToString();

    /// <summary>
    /// Error message if the model failed (optional).
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Provider name (NanoGPT or OpenRouter).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Converts a domain model result to this DTO.
    /// </summary>
    /// <param name="modelResult">The domain model result.</param>
    /// <returns>A new DTO instance.</returns>
    public static ModelResultDto FromDomainModel(ModelResult modelResult)
    {
        return new ModelResultDto
        {
            ModelId = modelResult.ModelId,
            Response = modelResult.Response,
            ResponseTimeMs = modelResult.ResponseTimeMs,
            TokenCount = modelResult.TokenCount,
            Status = modelResult.Status.ToString(),
            ErrorMessage = modelResult.ErrorMessage,
            Provider = modelResult.Provider
        };
    }

    /// <summary>
    /// Converts this DTO to a domain model result.
    /// </summary>
    /// <returns>A domain model result.</returns>
    public ModelResult ToDomainModel()
    {
        if (!Enum.TryParse<ModelResultStatus>(Status, out var statusEnum))
        {
            statusEnum = ModelResultStatus.Error; // Default to error if parsing fails
        }

        return ModelResult.Create(
            ModelId,
            Response,
            ResponseTimeMs,
            TokenCount,
            statusEnum,
            ErrorMessage,
            Provider);
    }
}
