using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Represents a model comparison operation in the domain.
/// </summary>
public class Comparison
{
    /// <summary>
    /// Unique identifier for this comparison.
    /// </summary>
    [Required]
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// The prompt that was sent to all models.
    /// </summary>
    [Required]
    public string Prompt { get; private set; } = string.Empty;

    /// <summary>
    /// List of model results from this comparison.
    /// </summary>
    public List<ModelResult> Results { get; private set; } = new();

    /// <summary>
    /// Timestamp when the comparison was executed.
    /// </summary>
    [Required]
    public DateTime ExecutedAt { get; private set; }

    /// <summary>
    /// Total number of models processed.
    /// </summary>
    public int TotalModels => Results.Count;

    /// <summary>
    /// Number of successful model responses.
    /// </summary>
    public int SuccessfulModels => Results.Count(r => r.Status == "success");

    /// <summary>
    /// Number of failed model responses.
    /// </summary>
    public int FailedModels => Results.Count(r => r.Status == "error");

    /// <summary>
    /// Average response time across all models (in milliseconds).
    /// </summary>
    public double AverageResponseTime => Results.Any(r => r.Status == "success")
        ? Results.Where(r => r.Status == "success").Average(r => r.ResponseTimeMs)
        : 0;

    /// <summary>
    /// Total tokens used across all models.
    /// </summary>
    public int TotalTokens => Results.Sum(r => r.TokenCount ?? 0);

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private Comparison() { }

    /// <summary>
    /// Creates a new comparison with the specified prompt.
    /// </summary>
    /// <param name="prompt">The prompt to send to all models.</param>
    /// <returns>A new comparison instance.</returns>
    public static Comparison Create(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        return new Comparison
        {
            Id = Guid.NewGuid().ToString(),
            Prompt = prompt,
            ExecutedAt = DateTime.UtcNow,
            Results = new List<ModelResult>()
        };
    }

    /// <summary>
    /// Adds a model result to this comparison.
    /// </summary>
    /// <param name="result">The model result to add.</param>
    public void AddResult(ModelResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        Results.Add(result);
    }

    /// <summary>
    /// Gets all results for a specific provider.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <returns>List of results for the specified provider.</returns>
    public List<ModelResult> GetResultsByProvider(string provider)
    {
        return Results.Where(r => r.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets the fastest successful response.
    /// </summary>
    /// <returns>The fastest successful model result, or null if no successful results.</returns>
    public ModelResult? GetFastestSuccessfulResponse()
    {
        return Results
            .Where(r => r.Status == "success")
            .OrderBy(r => r.ResponseTimeMs)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the slowest successful response.
    /// </summary>
    /// <returns>The slowest successful model result, or null if no successful results.</returns>
    public ModelResult? GetSlowestSuccessfulResponse()
    {
        return Results
            .Where(r => r.Status == "success")
            .OrderByDescending(r => r.ResponseTimeMs)
            .FirstOrDefault();
    }
}
