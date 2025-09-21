using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Infrastructure.Repositories;

/// <summary>
/// SQLite implementation of the evaluation repository using Entity Framework Core.
/// </summary>
public class SqliteEvaluationRepository : IEvaluationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SqliteEvaluationRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the SqliteEvaluationRepository.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public SqliteEvaluationRepository(ApplicationDbContext context, ILogger<SqliteEvaluationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Evaluation> SaveAsync(Evaluation evaluation, CancellationToken cancellationToken = default)
    {
        if (evaluation == null)
            throw new ArgumentNullException(nameof(evaluation));

        _logger.LogInformation("Saving evaluation {EvaluationId} for model {ModelId}", evaluation.Id, evaluation.ModelId);

        try
        {
            // Validate the evaluation before saving
            var validationErrors = evaluation.Validate();
            if (validationErrors.Any())
                throw new ArgumentException($"Invalid evaluation: {string.Join(", ", validationErrors)}");

            // Check if evaluation already exists
            var existing = _context.Evaluations
                .AsEnumerable()
                .FirstOrDefault(e => e.Id.Value == evaluation.Id.Value);

            if (existing != null)
            {
                // Update existing evaluation
                _context.Entry(existing).CurrentValues.SetValues(evaluation);
                existing.MarkAsSaved();
            }
            else
            {
                // Add new evaluation
                evaluation.MarkAsSaved();
                await _context.Evaluations.AddAsync(evaluation, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Evaluation {EvaluationId} saved successfully", evaluation.Id);

            return evaluation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save evaluation {EvaluationId}", evaluation.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Evaluation> UpdateAsync(Evaluation evaluation, CancellationToken cancellationToken = default)
    {
        if (evaluation == null)
            throw new ArgumentNullException(nameof(evaluation));

        _logger.LogInformation("Updating evaluation {EvaluationId}", evaluation.Id);

        try
        {
            // Validate the evaluation before updating
            var validationErrors = evaluation.Validate();
            if (validationErrors.Any())
                throw new ArgumentException($"Invalid evaluation: {string.Join(", ", validationErrors)}");

            var existing = _context.Evaluations
                .AsEnumerable()
                .FirstOrDefault(e => e.Id.Value == evaluation.Id.Value);

            if (existing == null)
                throw new KeyNotFoundException($"Evaluation with ID {evaluation.Id} not found");

            // Update the existing evaluation
            _context.Entry(existing).CurrentValues.SetValues(evaluation);
            existing.MarkAsSaved();

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Evaluation {EvaluationId} updated successfully", evaluation.Id);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update evaluation {EvaluationId}", evaluation.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Evaluation?> GetByIdAsync(EvaluationId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        _logger.LogDebug("Getting evaluation {EvaluationId}", id);

        try
        {
            return _context.Evaluations
                .AsEnumerable()
                .FirstOrDefault(e => e.Id.Value == id.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation {EvaluationId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Evaluation>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        _logger.LogDebug("Getting all evaluations (skip: {Skip}, take: {Take})", skip, take);

        try
        {
            return await _context.Evaluations
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all evaluations");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Evaluation>> GetByModelIdAsync(string modelId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        _logger.LogDebug("Getting evaluations for model {ModelId} (skip: {Skip}, take: {Take})", modelId, skip, take);

        try
        {
            return await _context.Evaluations
                .Where(e => e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluations for model {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Evaluation>> GetByPromptIdAsync(string promptId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptId))
            throw new ArgumentException("Prompt ID cannot be null or empty", nameof(promptId));
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        _logger.LogDebug("Getting evaluations for prompt {PromptId} (skip: {Skip}, take: {Take})", promptId, skip, take);

        try
        {
            return await _context.Evaluations
                .Where(e => e.PromptId.Equals(promptId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluations for prompt {PromptId}", promptId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Evaluation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date", nameof(startDate));
        if (skip < 0)
            throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take < 0)
            throw new ArgumentException("Take cannot be negative", nameof(take));

        _logger.LogDebug("Getting evaluations by date range {StartDate} to {EndDate} (skip: {Skip}, take: {Take})", startDate, endDate, skip, take);

        try
        {
            return await _context.Evaluations
                .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluations by date range");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(EvaluationId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        _logger.LogInformation("Deleting evaluation {EvaluationId}", id);

        try
        {
            var evaluation = _context.Evaluations
                .AsEnumerable()
                .FirstOrDefault(e => e.Id.Value == id.Value);

            if (evaluation == null)
                return false;

            _context.Evaluations.Remove(evaluation);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Evaluation {EvaluationId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete evaluation {EvaluationId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting total evaluation count");

        try
        {
            return await _context.Evaluations.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation count");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<double?> GetAverageRatingByModelIdAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        _logger.LogDebug("Getting average rating for model {ModelId}", modelId);

        try
        {
            return await _context.Evaluations
                .Where(e => e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase) && e.Rating.HasValue)
                .AverageAsync(e => e.Rating.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get average rating for model {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCountByModelIdAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        _logger.LogDebug("Getting evaluation count for model {ModelId}", modelId);

        try
        {
            return await _context.Evaluations
                .CountAsync(e => e.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation count for model {ModelId}", modelId);
            throw;
        }
    }
}