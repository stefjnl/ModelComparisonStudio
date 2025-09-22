using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Interfaces;

/// <summary>
/// Interface for model comparison repository operations.
/// </summary>
public interface IModelRepository
{
    /// <summary>
    /// Saves a comparison to the repository.
    /// </summary>
    /// <param name="comparison">The comparison to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved comparison.</returns>
    Task<Comparison> SaveComparisonAsync(
        Comparison comparison,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a comparison by ID.
    /// </summary>
    /// <param name="comparisonId">The comparison ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The comparison if found, null otherwise.</returns>
    Task<Comparison?> GetComparisonByIdAsync(
        string comparisonId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all comparisons with optional filtering.
    /// </summary>
    /// <param name="skip">Number of comparisons to skip.</param>
    /// <param name="take">Number of comparisons to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of comparisons.</returns>
    Task<IReadOnlyList<Comparison>> GetComparisonsAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comparisons by date range.
    /// </summary>
    /// <param name="startDate">Start date for the range.</param>
    /// <param name="endDate">End date for the range.</param>
    /// <param name="skip">Number of comparisons to skip.</param>
    /// <param name="take">Number of comparisons to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of comparisons within the date range.</returns>
    Task<IReadOnlyList<Comparison>> GetComparisonsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a comparison by ID.
    /// </summary>
    /// <param name="comparisonId">The comparison ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the comparison was deleted, false otherwise.</returns>
    Task<bool> DeleteComparisonAsync(
        string comparisonId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comparison statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison statistics.</returns>
    Task<ComparisonStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for provider repository operations.
/// </summary>
public interface IProviderRepository
{
    /// <summary>
    /// Gets all providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all providers.</returns>
    Task<IReadOnlyList<Provider>> GetAllProvidersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider by ID.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider if found, null otherwise.</returns>
    Task<Provider?> GetProviderByIdAsync(
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider by name.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider if found, null otherwise.</returns>
    Task<Provider?> GetProviderByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a provider to the repository.
    /// </summary>
    /// <param name="provider">The provider to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved provider.</returns>
    Task<Provider> SaveProviderAsync(
        Provider provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a provider.
    /// </summary>
    /// <param name="provider">The provider to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated provider.</returns>
    Task<Provider> UpdateProviderAsync(
        Provider provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a provider by ID.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider was deleted, false otherwise.</returns>
    Task<bool> DeleteProviderAsync(
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a provider exists by name.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider exists, false otherwise.</returns>
    Task<bool> ProviderExistsAsync(
        string name,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for comparison operations.
/// </summary>
public class ComparisonStatistics
{
    /// <summary>
    /// Total number of comparisons.
    /// </summary>
    public int TotalComparisons { get; set; }

    /// <summary>
    /// Number of successful comparisons.
    /// </summary>
    public int SuccessfulComparisons { get; set; }

    /// <summary>
    /// Number of failed comparisons.
    /// </summary>
    public int FailedComparisons { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Total tokens used across all comparisons.
    /// </summary>
    public int TotalTokensUsed { get; set; }

    /// <summary>
    /// Most used model.
    /// </summary>
    public string? MostUsedModel { get; set; }

    /// <summary>
    /// Most used provider.
    /// </summary>
    public string? MostUsedProvider { get; set; }

    /// <summary>
    /// Success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalComparisons > 0
        ? (double)SuccessfulComparisons / TotalComparisons * 100
        : 0;
}
