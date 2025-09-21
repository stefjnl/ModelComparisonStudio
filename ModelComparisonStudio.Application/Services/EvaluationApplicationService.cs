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
}