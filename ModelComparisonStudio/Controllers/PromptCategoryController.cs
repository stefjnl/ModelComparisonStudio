using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Application.DTOs;

namespace ModelComparisonStudio.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public class PromptCategoryController : BaseController
{
    private readonly PromptCategoryService _categoryService;

    public PromptCategoryController(
        PromptCategoryService categoryService,
        ILogger<PromptCategoryController> logger) : base(logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<PromptCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
            var dtos = categories.Select(PromptCategoryDto.FromDomainEntity);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Creates a new category
    /// </summary>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(PromptCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory([FromBody] CreatePromptCategoryDto requestDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateValidationErrorResponse(ModelState));
            }

            var category = await _categoryService.CreateCategoryAsync(
                name: requestDto.Name,
                description: requestDto.Description ?? string.Empty,
                color: requestDto.Color,
                cancellationToken: cancellationToken);

            var dto = PromptCategoryDto.FromDomainEntity(category);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating category");
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Updates a category
    /// </summary>
    [HttpPut("categories/{id}")]
    [ProducesResponseType(typeof(PromptCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] UpdatePromptCategoryDto requestDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateValidationErrorResponse(ModelState));
            }

            var category = await _categoryService.UpdateCategoryAsync(
                id: id,
                name: requestDto.Name,
                description: requestDto.Description,
                color: requestDto.Color,
                cancellationToken: cancellationToken);

            var dto = PromptCategoryDto.FromDomainEntity(category);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating category: {CategoryId}", id);
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {CategoryId}", id);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Deletes a category
    /// </summary>
    [HttpDelete("categories/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCategory(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _categoryService.DeleteCategoryAsync(id, cancellationToken);
            if (!success)
            {
                return BadRequest($"Cannot delete category {id} - it may still contain templates or not exist");
            }

            return Ok(new { message = $"Category {id} deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error deleting category: {CategoryId}", id);
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    }