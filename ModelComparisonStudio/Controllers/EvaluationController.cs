
using Microsoft.AspNetCore.Mvc;
using ModelComparisonStudio.Application.DTOs;
using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Controllers;

[ApiController]
[Route("api/evaluations")]
public class EvaluationController : ControllerBase
{
    private readonly EvaluationApplicationService _evaluationService;
    private readonly ILogger<EvaluationController> _logger;

    public EvaluationController(
        EvaluationApplicationService evaluationService,
        ILogger<EvaluationController> logger)
    {
        _evaluationService = evaluationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new evaluation record.
    /// </summary>
    /// <param name="dto">The evaluation creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created evaluation.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEvaluation(
        [FromBody] CreateEvaluationDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    type = "validation_error",
                    title = "Validation Error",
                    status = 400,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var evaluation = await _evaluationService.CreateEvaluationAsync(dto, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetEvaluationById),
                new { evaluationId = evaluation.Id },
                evaluation);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating evaluation");
            return BadRequest(new
            {
                type = "validation_error",
                title = "Validation Error",
                status = 400,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating evaluation");
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while creating the evaluation",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates the rating for an existing evaluation.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="dto">The rating update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated evaluation.</returns>
    [HttpPut("{evaluationId}/rating")]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRating(
        [FromRoute] string evaluationId,
        [FromBody] UpdateRatingDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    type = "validation_error",
                    title = "Validation Error",
                    status = 400,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var evaluation = await _evaluationService.UpdateRatingAsync(evaluationId, dto, cancellationToken);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Evaluation not found: {EvaluationId}", evaluationId);
            return NotFound(new
            {
                type = "not_found",
                title = "Not Found",
                status = 404,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating rating for evaluation {EvaluationId}", evaluationId);
            return BadRequest(new
            {
                type = "validation_error",
                title = "Validation Error",
                status = 400,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating rating for evaluation {EvaluationId}", evaluationId);
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while updating the rating",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Updates the comment for an existing evaluation.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="dto">The comment update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated evaluation.</returns>
    [HttpPut("{evaluationId}/comment")]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateComment(
        [FromRoute] string evaluationId,
        [FromBody] UpdateCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    type = "validation_error",
                    title = "Validation Error",
                    status = 400,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage),
                    traceId = HttpContext.TraceIdentifier
                });
            }

            var evaluation = await _evaluationService.UpdateCommentAsync(evaluationId, dto, cancellationToken);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Evaluation not found: {EvaluationId}", evaluationId);
            return NotFound(new
            {
                type = "not_found",
                title = "Not Found",
                status = 404,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating comment for evaluation {EvaluationId}", evaluationId);
            return BadRequest(new
            {
                type = "validation_error",
                title = "Validation Error",
                status = 400,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating comment for evaluation {EvaluationId}", evaluationId);
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while updating the comment",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets an evaluation by ID.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation.</returns>
    [HttpGet("{evaluationId}")]
    [ProducesResponseType(typeof(EvaluationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEvaluationById(
        [FromRoute] string evaluationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evaluation = await _evaluationService.GetEvaluationByIdAsync(evaluationId, cancellationToken);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Evaluation not found: {EvaluationId}", evaluationId);
            return NotFound(new
            {
                type = "not_found",
                title = "Not Found",
                status = 404,
                detail = ex.Message,
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting evaluation {EvaluationId}", evaluationId);
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while retrieving the evaluation",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets all evaluations with optional pagination.
    /// </summary>
    /// <param name="skip">Number of evaluations to skip (default: 0).</param>
    /// <param name="take">Number of evaluations to take (default: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EvaluationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllEvaluations(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evaluations = await _evaluationService.GetAllEvaluationsAsync(skip, take, cancellationToken);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting all evaluations");
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while retrieving evaluations",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets evaluations for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="skip">Number of evaluations to skip (default: 0).</param>
    /// <param name="take">Number of evaluations to take (default: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evaluations for the specified model.</returns>
    [HttpGet("model/{modelId}")]
    [ProducesResponseType(typeof(IReadOnlyList<EvaluationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEvaluationsByModelId(
        [FromRoute] string modelId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evaluations = await _evaluationService.GetEvaluationsByModelIdAsync(modelId, skip, take, cancellationToken);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting evaluations for model {ModelId}", modelId);
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while retrieving evaluations",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Gets evaluation statistics for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Evaluation statistics for the specified model.</returns>
    [HttpGet("statistics/model/{modelId}")]
    [ProducesResponseType(typeof(EvaluationStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEvaluationStatistics(
        [FromRoute] string modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _evaluationService.GetEvaluationStatisticsAsync(modelId, cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting evaluation statistics for model {ModelId}", modelId);
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while retrieving evaluation statistics",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Deletes an evaluation by ID.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the evaluation was deleted, false otherwise.</returns>
    [HttpDelete("{evaluationId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEvaluation(
        [FromRoute] string evaluationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _evaluationService.DeleteEvaluationAsync(evaluationId, cancellationToken);
            
            if (!result)
            {
                return NotFound(new
                {
                    type = "not_found",
                    title = "Not Found",
                    status = 404,
                    detail = $"Evaluation with ID {evaluationId} not found",
                    traceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting evaluation {EvaluationId}", evaluationId);
            return StatusCode(500, new
            {
                type = "internal_error",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred while deleting the evaluation",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}