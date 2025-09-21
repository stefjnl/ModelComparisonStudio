using System.Collections.Concurrent;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the evaluation repository for development and testing.
/// </summary>
public class InMemoryEvaluationRepository : IEvaluationRepository
{
    private readonly ConcurrentDictionary<string, Evaluation> _evaluations = new();
    private int _totalCount = 0;

    /// <inheritdoc />
    public Task<Evaluation> SaveAsync(Evaluation evaluation, CancellationToken cancellationToken = default)
    {
        if (evaluation == null)
            throw new ArgumentNullException(nameof(evaluation));

        // Validate the evaluation before saving
        var validationErrors = evaluation.Validate();
        if (validationErrors.Any())
            throw new ArgumentException($"Invalid evaluation: {string.Join(", ", validationErrors)}");

        // If the evaluation already exists, treat as update
        if (_evaluations.ContainsKey(evaluation.Id.ToString()))
        {
            return UpdateAsync(evaluation, cancellationToken);
        }

        // Mark as saved and add to dictionary
        evaluation.MarkAsSaved();
        _evaluations[evaluation.Id.ToString()] = evaluation;
        Interlocked.Increment(ref _totalCount);

        return Task.FromResult(evaluation);
    }

    /// <inheritdoc />
    public Task<Evaluation> UpdateAsync(Evaluation evaluation, CancellationToken cancellationToken = default)
    {
        if (evaluation == null)
            throw new ArgumentNullException(nameof(evaluation));

        // Validate the evaluation before updating
        var validationErrors = evaluation.Validate();
        if (validationErrors.Any())
            throw new ArgumentException($"Invalid evaluation: {string.Join(", ", validationErrors)}");

        var evaluationId = evaluation.Id.ToString();
        if (!_evaluations.ContainsKey(evaluationId))
            throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");

        // Mark as saved and update in dictionary
        evaluation.MarkAsSaved();
        _evaluations[evaluationId] = evaluation;

        return Task.FromResult(evaluation);
    }

    /// <inheritdoc />
    public Task<Evaluation?> GetByIdAsync(EvaluationId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        _evaluations.TryGetValue(id.ToString(), out var evaluation);
        return Task.FromResult(evaluation);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Evaluation>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        var evaluations = _evaluations.Values
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<Evaluation>>(evaluations);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Evaluation>> GetByModelIdAsync(string modelId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        var evaluations = _evaluations.Values
            .Where(e => e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<Evaluation>>(evaluations);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Evaluation>> GetByPromptIdAsync(string promptId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptId))
            throw new ArgumentException("Prompt ID cannot be null or empty", nameof(promptId));
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        var evaluations = _evaluations.Values
            .Where(e => e.PromptId.Equals(promptId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<Evaluation>>(evaluations);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Evaluation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date", nameof(startDate));
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        var evaluations = _evaluations.Values
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult<IReadOnlyList<Evaluation>>(evaluations);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(EvaluationId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        var removed = _evaluations.TryRemove(id.ToString(), out _);
        if (removed)
        {
            Interlocked.Decrement(ref _totalCount);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_totalCount);
    }

    /// <inheritdoc />
    public Task<double?> GetAverageRatingByModelIdAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        var ratings = _evaluations.Values
            .Where(e => e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase) && e.Rating.HasValue)
            .Select(e => e.Rating.Value)
            .ToList();

        if (!ratings.Any())
            return Task.FromResult<double?>(null);

        var average = ratings.Average();
        return Task.FromResult<double?>(average);
    }

    /// <inheritdoc />
    public Task<int> GetCountByModelIdAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        var count = _evaluations.Values
            .Count(e => e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<Evaluation?> GetByPromptIdAndModelIdAsync(string promptId, string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptId))
            throw new ArgumentException("Prompt ID cannot be null or empty", nameof(promptId));
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        var evaluation = _evaluations.Values
            .FirstOrDefault(e =>
                e.PromptId.Equals(promptId, StringComparison.OrdinalIgnoreCase) &&
                e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(evaluation);
    }

    /// <summary>
    /// Clears all evaluations from the repository (for testing purposes).
    /// </summary>
    public void Clear()
    {
        _evaluations.Clear();
        _totalCount = 0;
    }
}