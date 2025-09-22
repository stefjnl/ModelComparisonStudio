using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Interfaces;

/// <summary>
/// Interface for evaluation repository operations.
/// </summary>
public interface IEvaluationRepository
{
    /// <summary>
    /// Saves an evaluation to the repository.
    /// </summary>
    /// <param name="evaluation">The evaluation to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved evaluation.</returns>
    Task<Evaluation> SaveAsync(Evaluation evaluation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing evaluation in the repository.
    /// </summary>
    /// <param name="evaluation">The evaluation to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated evaluation.</returns>
    Task<Evaluation> UpdateAsync(Evaluation evaluation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an evaluation by its ID.
    /// </summary>
    /// <param name="id">The evaluation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation if found, null otherwise.</returns>
    Task<Evaluation?> GetByIdAsync(EvaluationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all evaluations with optional filtering and pagination.
    /// </summary>
    /// <param name="skip">Number of evaluations to skip.</param>
    /// <param name="take">Number of evaluations to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations.</returns>
    Task<IReadOnlyList<Evaluation>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all evaluations without pagination (for statistics and reporting).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All evaluations in the repository.</returns>
    Task<IReadOnlyList<Evaluation>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets evaluations by model ID.
    /// </summary>
    /// <param name="modelId">The model ID to filter by.</param>
    /// <param name="skip">Number of evaluations to skip.</param>
    /// <param name="take">Number of evaluations to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations for the specified model.</returns>
    Task<IReadOnlyList<Evaluation>> GetByModelIdAsync(string modelId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets evaluations by prompt ID.
    /// </summary>
    /// <param name="promptId">The prompt ID to filter by.</param>
    /// <param name="skip">Number of evaluations to skip.</param>
    /// <param name="take">Number of evaluations to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations for the specified prompt.</returns>
    Task<IReadOnlyList<Evaluation>> GetByPromptIdAsync(string promptId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an evaluation by prompt ID and model ID.
    /// </summary>
    /// <param name="promptId">The prompt ID.</param>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation if found, null otherwise.</returns>
    Task<Evaluation?> GetByPromptIdAndModelIdAsync(string promptId, string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets evaluations within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="skip">Number of evaluations to skip.</param>
    /// <param name="take">Number of evaluations to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations within the date range.</returns>
    Task<IReadOnlyList<Evaluation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an evaluation by its ID.
    /// </summary>
    /// <param name="id">The evaluation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the evaluation was deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(EvaluationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of evaluations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of evaluations.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The average rating for the model, or null if no ratings exist.</returns>
    Task<double?> GetAverageRatingByModelIdAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of evaluations for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of evaluations for the model.</returns>
    Task<int> GetCountByModelIdAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all evaluations since a specific date.
    /// </summary>
    /// <param name="sinceDate">The starting date to filter evaluations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations since the specified date.</returns>
    Task<IReadOnlyList<Evaluation>> GetAllSinceAsync(DateTime sinceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all evaluations for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID to delete evaluations for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of evaluations deleted.</returns>
    Task<int> DeleteByModelIdAsync(string modelId, CancellationToken cancellationToken = default);
}
