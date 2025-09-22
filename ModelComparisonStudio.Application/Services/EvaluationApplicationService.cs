using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.DTOs;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Application.Services;

/// <summary>
/// Application service for handling evaluation operations.
/// </summary>
public class EvaluationApplicationService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly ILogger<EvaluationApplicationService> _logger;

    public EvaluationApplicationService(
        IEvaluationRepository evaluationRepository,
        ILogger<EvaluationApplicationService> logger)
    {
        _evaluationRepository = evaluationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new evaluation.
    /// </summary>
    /// <param name="dto">The evaluation creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created evaluation DTO.</returns>
    public async Task<EvaluationDto> CreateEvaluationAsync(
        CreateEvaluationDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating evaluation for model {ModelId} with prompt {PromptId}",
            dto.ModelId, dto.PromptId);

        try
        {
            var evaluation = Evaluation.Create(
                dto.PromptId,
                dto.PromptText,
                dto.ModelId,
                dto.ResponseTimeMs, // Already has default value of 1000
                dto.TokenCount);

            // Set rating if provided
            if (dto.Rating.HasValue)
            {
                evaluation.UpdateRating(dto.Rating.Value);
            }

            // Set comment if provided
            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                evaluation.UpdateComment(dto.Comment);
            }

            var savedEvaluation = await _evaluationRepository.SaveAsync(evaluation, cancellationToken);

            _logger.LogInformation("Evaluation {EvaluationId} created successfully", savedEvaluation.Id);

            return EvaluationDto.FromDomainEvaluation(savedEvaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create evaluation for model {ModelId}", dto.ModelId);
            throw;
        }
    }

    /// <summary>
    /// Updates the rating for an existing evaluation.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="dto">The rating update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated evaluation DTO.</returns>
    public async Task<EvaluationDto> UpdateRatingAsync(
        string evaluationId,
        UpdateRatingDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating rating for evaluation {EvaluationId} to {Rating}",
            evaluationId, dto.Rating);

        try
        {
            var id = EvaluationId.FromString(evaluationId);
            var evaluation = await _evaluationRepository.GetByIdAsync(id, cancellationToken);

            if (evaluation == null)
            {
                throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");
            }

            evaluation.UpdateRating(dto.Rating);
            var updatedEvaluation = await _evaluationRepository.UpdateAsync(evaluation, cancellationToken);

            _logger.LogInformation("Rating updated successfully for evaluation {EvaluationId}", evaluationId);

            return EvaluationDto.FromDomainEvaluation(updatedEvaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update rating for evaluation {EvaluationId}", evaluationId);
            throw;
        }
    }

    /// <summary>
    /// Updates the comment for an existing evaluation.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="dto">The comment update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated evaluation DTO.</returns>
    public async Task<EvaluationDto> UpdateCommentAsync(
        string evaluationId,
        UpdateCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating comment for evaluation {EvaluationId}", evaluationId);

        try
        {
            var id = EvaluationId.FromString(evaluationId);
            var evaluation = await _evaluationRepository.GetByIdAsync(id, cancellationToken);

            if (evaluation == null)
            {
                throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");
            }

            evaluation.UpdateComment(dto.Comment);
            var updatedEvaluation = await _evaluationRepository.UpdateAsync(evaluation, cancellationToken);

            _logger.LogInformation("Comment updated successfully for evaluation {EvaluationId}", evaluationId);

            return EvaluationDto.FromDomainEvaluation(updatedEvaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update comment for evaluation {EvaluationId}", evaluationId);
            throw;
        }
    }

    /// <summary>
    /// Gets an evaluation by ID.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation DTO.</returns>
    public async Task<EvaluationDto> GetEvaluationByIdAsync(
        string evaluationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting evaluation {EvaluationId}", evaluationId);

        try
        {
            var id = EvaluationId.FromString(evaluationId);
            var evaluation = await _evaluationRepository.GetByIdAsync(id, cancellationToken);

            if (evaluation == null)
            {
                throw new KeyNotFoundException($"Evaluation with ID {evaluationId} not found");
            }

            return EvaluationDto.FromDomainEvaluation(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation {EvaluationId}", evaluationId);
            throw;
        }
    }

    /// <summary>
    /// Gets all evaluations with optional pagination.
    /// </summary>
    /// <param name="skip">Number of evaluations to skip.</param>
    /// <param name="take">Number of evaluations to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluation DTOs.</returns>
    public async Task<IReadOnlyList<EvaluationDto>> GetAllEvaluationsAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all evaluations (skip: {Skip}, take: {Take})", skip, take);

        try
        {
            var evaluations = await _evaluationRepository.GetAllAsync(skip, take, cancellationToken);
            return evaluations.Select(EvaluationDto.FromDomainEvaluation).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all evaluations");
            throw;
        }
    }

    /// <summary>
    /// Gets evaluations by model ID.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="skip">Number of evaluations to skip.</param>
    /// <param name="take">Number of evaluations to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluation DTOs for the specified model.</returns>
    public async Task<IReadOnlyList<EvaluationDto>> GetEvaluationsByModelIdAsync(
        string modelId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting evaluations for model {ModelId} (skip: {Skip}, take: {Take})",
            modelId, skip, take);

        try
        {
            var evaluations = await _evaluationRepository.GetByModelIdAsync(modelId, skip, take, cancellationToken);
            return evaluations.Select(EvaluationDto.FromDomainEvaluation).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluations for model {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// Gets evaluation statistics for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Evaluation statistics DTO.</returns>
    public async Task<EvaluationStatisticsDto> GetEvaluationStatisticsAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting evaluation statistics for model {ModelId}", modelId);

        try
        {
            var averageRating = await _evaluationRepository.GetAverageRatingByModelIdAsync(modelId, cancellationToken);
            var totalEvaluations = await _evaluationRepository.GetCountByModelIdAsync(modelId, cancellationToken);

            // For in-memory repository, we need to calculate commented evaluations manually
            var evaluations = await _evaluationRepository.GetByModelIdAsync(modelId, 0, int.MaxValue, cancellationToken);
            var ratedEvaluations = evaluations.Count(e => e.Rating.HasValue);
            var commentedEvaluations = evaluations.Count(e => !e.Comment.IsEmpty());

            return new EvaluationStatisticsDto
            {
                ModelId = modelId,
                AverageRating = averageRating,
                TotalEvaluations = totalEvaluations,
                RatedEvaluations = ratedEvaluations,
                CommentedEvaluations = commentedEvaluations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation statistics for model {ModelId}", modelId);
            throw;
        }
    }

    /// <summary>
    /// Creates or updates an evaluation based on prompt ID and model ID.
    /// If an evaluation exists for the same prompt and model, it will be updated.
    /// Otherwise, a new evaluation will be created.
    /// </summary>
    /// <param name="dto">The evaluation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated evaluation DTO.</returns>
    public async Task<EvaluationDto> UpsertEvaluationAsync(
        CreateEvaluationDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upserting evaluation for model {ModelId} with prompt {PromptId}",
            dto.ModelId, dto.PromptId);

        try
        {
            // Check if evaluation already exists
            var existingEvaluation = await _evaluationRepository.GetByPromptIdAndModelIdAsync(
                dto.PromptId, dto.ModelId, cancellationToken);

            if (existingEvaluation != null)
            {
                // Update existing evaluation
                _logger.LogInformation("Updating existing evaluation {EvaluationId}. Current ResponseTimeMs: {CurrentResponseTimeMs}",
                    existingEvaluation.Id, existingEvaluation.ResponseTimeMs);

                // Update response time and token count (these are not updated by the UpdateRating/UpdateComment methods)
                _logger.LogDebug("Updating ResponseTimeMs: {ResponseTimeMs}, TokenCount: {TokenCount} for evaluation {EvaluationId}",
                    dto.ResponseTimeMs, dto.TokenCount, existingEvaluation.Id);
                existingEvaluation.UpdateResponseTimeAndTokenCount(dto.ResponseTimeMs, dto.TokenCount);
                _logger.LogDebug("After update - ResponseTimeMs: {ResponseTimeMs}, TokenCount: {TokenCount}",
                    existingEvaluation.ResponseTimeMs, existingEvaluation.TokenCount);

                // Update rating if provided
                if (dto.Rating.HasValue)
                {
                    existingEvaluation.UpdateRating(dto.Rating.Value);
                }

                // Update comment if provided
                if (!string.IsNullOrWhiteSpace(dto.Comment))
                {
                    existingEvaluation.UpdateComment(dto.Comment);
                }

                var updatedEvaluation = await _evaluationRepository.UpdateAsync(existingEvaluation, cancellationToken);
                _logger.LogInformation("Evaluation {EvaluationId} updated successfully", existingEvaluation.Id);

                return EvaluationDto.FromDomainEvaluation(updatedEvaluation);
            }
            else
            {
                // Create new evaluation
                _logger.LogInformation("Creating new evaluation for model {ModelId} with prompt {PromptId}, ResponseTimeMs: {ResponseTimeMs}",
                    dto.ModelId, dto.PromptId, dto.ResponseTimeMs);

                var evaluation = Evaluation.Create(
                    dto.PromptId,
                    dto.PromptText,
                    dto.ModelId,
                    dto.ResponseTimeMs, // Already has default value of 1000
                    dto.TokenCount);

                // Set rating if provided
                if (dto.Rating.HasValue)
                {
                    evaluation.UpdateRating(dto.Rating.Value);
                }

                // Set comment if provided
                if (!string.IsNullOrWhiteSpace(dto.Comment))
                {
                    evaluation.UpdateComment(dto.Comment);
                }

                var savedEvaluation = await _evaluationRepository.SaveAsync(evaluation, cancellationToken);

                _logger.LogInformation("Evaluation {EvaluationId} created successfully", savedEvaluation.Id);

                return EvaluationDto.FromDomainEvaluation(savedEvaluation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert evaluation for model {ModelId}", dto.ModelId);
            throw;
        }
    }

    /// <summary>
    /// Gets evaluation statistics for all models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluation statistics for all models.</returns>
    public async Task<IReadOnlyList<EvaluationStatisticsDto>> GetAllEvaluationStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting evaluation statistics for all models");

        try
        {
            var allEvaluations = await _evaluationRepository.GetAllAsync(0, int.MaxValue, cancellationToken);

            // Group evaluations by model ID
            var modelGroups = allEvaluations
                .GroupBy(e => e.ModelId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var statistics = new List<EvaluationStatisticsDto>();

            foreach (var modelGroup in modelGroups)
            {
                var modelId = modelGroup.Key;
                var evaluations = modelGroup.Value;

                var averageRating = await _evaluationRepository.GetAverageRatingByModelIdAsync(modelId, cancellationToken);
                var totalEvaluations = evaluations.Count;
                var ratedEvaluations = evaluations.Count(e => e.Rating.HasValue);
                var commentedEvaluations = evaluations.Count(e => !e.Comment.IsEmpty());

                // Calculate average response time and token count
                var evaluationsWithResponseTime = evaluations.Where(e => e.ResponseTimeMs > 0);
                var averageSpeed = evaluationsWithResponseTime.Any()
                    ? evaluationsWithResponseTime.Average(e => e.ResponseTimeMs)
                    : 0;

                var evaluationsWithTokens = evaluations.Where(e => e.TokenCount.HasValue && e.TokenCount.Value > 0);
                var averageTokens = evaluationsWithTokens.Any()
                    ? evaluationsWithTokens.Average(e => e.TokenCount.Value)
                    : 0;

                // Calculate comment rate
                var commentRate = totalEvaluations > 0
                    ? (double)commentedEvaluations / totalEvaluations * 100
                    : 0;

                // Calculate days since last evaluation
                var lastEvaluated = evaluations.Any()
                    ? (DateTime.UtcNow - evaluations.Max(e => e.UpdatedAt)).Days
                    : 0;

                // Calculate rating distribution (1-10 stars)
                var ratingDistribution = new int[10];
                foreach (var evaluation in evaluations.Where(e => e.Rating.HasValue))
                {
                    var rating = evaluation.Rating.Value;
                    if (rating >= 1 && rating <= 10)
                    {
                        ratingDistribution[rating - 1]++;
                    }
                }

                statistics.Add(new EvaluationStatisticsDto
                {
                    ModelId = modelId,
                    AverageRating = averageRating,
                    TotalEvaluations = totalEvaluations,
                    RatedEvaluations = ratedEvaluations,
                    CommentedEvaluations = commentedEvaluations,
                    AverageSpeed = averageSpeed,
                    AverageTokens = averageTokens,
                    CommentRate = commentRate,
                    LastEvaluated = lastEvaluated,
                    RatingDistribution = ratingDistribution
                });
            }

            _logger.LogInformation("Retrieved statistics for {ModelCount} models", statistics.Count);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation statistics for all models");
            throw;
        }
    }

    /// <summary>
    /// Gets evaluation statistics for all models with timeframe filtering.
    /// </summary>
    /// <param name="timeframe">Timeframe filter: all, week, or month.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluation statistics for all models within the specified timeframe.</returns>
    public async Task<IReadOnlyList<EvaluationStatisticsDto>> GetAllEvaluationStatisticsByTimeframeAsync(
        string timeframe = "all",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting evaluation statistics for all models with timeframe: {Timeframe}", timeframe);

        try
        {
            DateTime? startDate = null;
            switch (timeframe.ToLower())
            {
                case "week":
                    startDate = DateTime.UtcNow.AddDays(-7);
                    break;
                case "month":
                    startDate = DateTime.UtcNow.AddDays(-30);
                    break;
                case "all":
                default:
                    startDate = null;
                    break;
            }

            var allEvaluations = startDate.HasValue
                ? await _evaluationRepository.GetAllSinceAsync(startDate.Value, cancellationToken)
                : await _evaluationRepository.GetAllAsync(0, int.MaxValue, cancellationToken);

            // Group evaluations by model ID
            var modelGroups = allEvaluations
                .GroupBy(e => e.ModelId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var statistics = new List<EvaluationStatisticsDto>();

            foreach (var modelGroup in modelGroups)
            {
                var modelId = modelGroup.Key;
                var evaluations = modelGroup.Value;

                var averageRating = await _evaluationRepository.GetAverageRatingByModelIdAsync(modelId, cancellationToken);
                var totalEvaluations = evaluations.Count;
                var ratedEvaluations = evaluations.Count(e => e.Rating.HasValue);
                var commentedEvaluations = evaluations.Count(e => !e.Comment.IsEmpty());

                // Calculate average response time and token count
                var evaluationsWithResponseTime = evaluations.Where(e => e.ResponseTimeMs > 0);
                var averageSpeed = evaluationsWithResponseTime.Any()
                    ? evaluationsWithResponseTime.Average(e => e.ResponseTimeMs)
                    : 0;

                var evaluationsWithTokens = evaluations.Where(e => e.TokenCount.HasValue && e.TokenCount.Value > 0);
                var averageTokens = evaluationsWithTokens.Any()
                    ? evaluationsWithTokens.Average(e => e.TokenCount.Value)
                    : 0;

                // Calculate comment rate
                var commentRate = totalEvaluations > 0
                    ? (double)commentedEvaluations / totalEvaluations * 100
                    : 0;

                // Calculate days since last evaluation
                var lastEvaluated = evaluations.Any()
                    ? (DateTime.UtcNow - evaluations.Max(e => e.UpdatedAt)).Days
                    : 0;

                // Calculate rating distribution (1-10 stars)
                var ratingDistribution = new int[10];
                foreach (var evaluation in evaluations.Where(e => e.Rating.HasValue))
                {
                    var rating = evaluation.Rating.Value;
                    if (rating >= 1 && rating <= 10)
                    {
                        ratingDistribution[rating - 1]++;
                    }
                }

                statistics.Add(new EvaluationStatisticsDto
                {
                    ModelId = modelId,
                    AverageRating = averageRating,
                    TotalEvaluations = totalEvaluations,
                    RatedEvaluations = ratedEvaluations,
                    CommentedEvaluations = commentedEvaluations,
                    AverageSpeed = averageSpeed,
                    AverageTokens = averageTokens,
                    CommentRate = commentRate,
                    LastEvaluated = lastEvaluated,
                    RatingDistribution = ratingDistribution
                });
            }

            _logger.LogInformation("Retrieved statistics for {ModelCount} models with timeframe {Timeframe}", statistics.Count, timeframe);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation statistics for all models with timeframe {Timeframe}", timeframe);
            throw;
        }
    }

    /// <summary>
    /// Deletes an evaluation by ID.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the evaluation was deleted, false otherwise.</returns>
    public async Task<bool> DeleteEvaluationAsync(
        string evaluationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting evaluation {EvaluationId}", evaluationId);

        try
        {
            var id = EvaluationId.FromString(evaluationId);
            return await _evaluationRepository.DeleteAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete evaluation {EvaluationId}", evaluationId);
            throw;
        }
    }

    /// <summary>
    /// Deletes all evaluations for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID to delete evaluations for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of evaluations deleted.</returns>
    public async Task<int> DeleteEvaluationsByModelIdAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all evaluations for model {ModelId}", modelId);

        try
        {
            var deletedCount = await _evaluationRepository.DeleteByModelIdAsync(modelId, cancellationToken);

            _logger.LogInformation("Successfully deleted {Count} evaluations for model {ModelId}",
                deletedCount, modelId);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete evaluations for model {ModelId}", modelId);
            throw;
        }
    }
}
