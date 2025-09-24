using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Application.DTOs;

namespace ModelComparisonStudio.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public class PromptTemplateController : BaseController
{
    private readonly PromptTemplateService _templateService;
    private readonly PromptCategoryService _categoryService;

    public PromptTemplateController(
        PromptTemplateService templateService,
        PromptCategoryService categoryService,
        ILogger<PromptTemplateController> logger) : base(logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    #region Template Operations

    /// <summary>
    /// Gets all prompt templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllTemplates(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _templateService.GetAllTemplatesWithCategoriesAsync(cancellationToken);
            var templates = result.Templates;
            var categories = result.Categories;

            var categoryLookup = categories.ToDictionary(c => c.Id);

            var dtos = templates.Select(t =>
            {
                categoryLookup.TryGetValue(t.Category, out var category);
                return PromptTemplateDto.FromDomainEntity(t, category);
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all templates");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    [HttpGet("templates/{id}")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTemplateById(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(id, cancellationToken);
            if (template == null)
            {
                return NotFound($"Template with ID {id} not found");
            }

            var categories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
            var categoryObj = categories.FirstOrDefault(c => c.Id == template.Category);

            var dto = PromptTemplateDto.FromDomainEntity(template, categoryObj);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template by ID: {TemplateId}", id);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Gets templates by category
    /// </summary>
    [HttpGet("templates/category/{categoryId}")]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTemplatesByCategory(string categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _templateService.GetTemplatesByCategoryWithCategoriesAsync(categoryId, cancellationToken);
            var templates = result.Templates;
            var categories = result.Categories;

            var categoryLookup = categories.ToDictionary(c => c.Id);
            var categoryObj = categoryLookup.GetValueOrDefault(categoryId);

            var dtos = templates.Select(t => PromptTemplateDto.FromDomainEntity(t, categoryObj));
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates by category: {CategoryId}", categoryId);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Searches templates by name or content
    /// </summary>
    [HttpGet("templates/search")]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchTemplates([FromQuery] string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _templateService.SearchTemplatesWithCategoriesAsync(searchTerm, cancellationToken);
            var templates = result.Templates;
            var categories = result.Categories;

            var categoryLookup = categories.ToDictionary(c => c.Id);

            var dtos = templates.Select(t =>
            {
                categoryLookup.TryGetValue(t.Category, out var category);
                return PromptTemplateDto.FromDomainEntity(t, category);
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates with term: {SearchTerm}", searchTerm);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Gets favorite templates
    /// </summary>
    [HttpGet("templates/favorites")]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFavoriteTemplates(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _templateService.GetFavoriteTemplatesWithCategoriesAsync(cancellationToken);
            var templates = result.Templates;
            var categories = result.Categories;

            var categoryLookup = categories.ToDictionary(c => c.Id);

            var dtos = templates.Select(t =>
            {
                categoryLookup.TryGetValue(t.Category, out var category);
                return PromptTemplateDto.FromDomainEntity(t, category);
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite templates");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Creates a new template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreatePromptTemplateDto requestDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateValidationErrorResponse(ModelState));
            }

            var template = await _templateService.CreateTemplateAsync(
                title: requestDto.Title,
                description: requestDto.Description ?? string.Empty,
                content: requestDto.Content,
                categoryId: requestDto.Category,
                variables: requestDto.Variables?.Select(v => v.ToDomainEntity()).ToList(),
                isSystemTemplate: requestDto.IsSystemTemplate,
                cancellationToken: cancellationToken);

            var allCategories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
            var categoryObj = allCategories.FirstOrDefault(c => c.Id == template.Category);

            var dto = PromptTemplateDto.FromDomainEntity(template, categoryObj);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating template");
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Updates an existing template
    /// </summary>
    [HttpPut("templates/{id}")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTemplate(string id, [FromBody] UpdatePromptTemplateDto requestDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateValidationErrorResponse(ModelState));
            }

            var template = await _templateService.UpdateTemplateAsync(
                id: id,
                title: requestDto.Title,
                description: requestDto.Description,
                content: requestDto.Content,
                categoryId: requestDto.Category,
                variables: requestDto.Variables?.Select(v => v.ToDomainEntity()).ToList(),
                cancellationToken: cancellationToken);

            var allCategories = await _categoryService.GetAllCategoriesAsync(cancellationToken);
            var categoryObj = allCategories.FirstOrDefault(c => c.Id == template.Category);

            var dto = PromptTemplateDto.FromDomainEntity(template, categoryObj);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating template: {TemplateId}", id);
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot modify system template: {TemplateId}", id);
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template: {TemplateId}", id);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Deletes a template
    /// </summary>
    [HttpDelete("templates/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTemplate(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _templateService.DeleteTemplateAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound($"Template with ID {id} not found");
            }

            return Ok(new { message = $"Template {id} deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error deleting template: {TemplateId}", id);
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template: {TemplateId}", id);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Toggles the favorite status of a template
    /// </summary>
    [HttpPost("templates/{id}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ToggleFavorite(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _templateService.ToggleTemplateFavoriteAsync(id, cancellationToken);
            if (!success)
            {
                return BadRequest($"Failed to toggle favorite status for template {id}");
            }

            return Ok(new { message = $"Template {id} favorite status updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite status for template: {TemplateId}", id);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Expands a template with variable values
    /// </summary>
    [HttpPost("templates/expand")]
    [ProducesResponseType(typeof(ExpandedTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExpandTemplate([FromBody] ExpandTemplateDto requestDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(CreateValidationErrorResponse(ModelState));
            }

            var expandedContent = await _templateService.ExpandTemplateAsync(
                requestDto.TemplateId,
                requestDto.VariableValues,
                cancellationToken);

            var template = await _templateService.GetTemplateByIdAsync(requestDto.TemplateId, cancellationToken);

            var dto = new ExpandedTemplateDto
            {
                Content = expandedContent,
                TemplateId = requestDto.TemplateId,
                TemplateTitle = template?.Title ?? "Unknown"
            };

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error expanding template: {TemplateId}", requestDto.TemplateId);
            return BadRequest(CreateValidationErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding template: {TemplateId}", requestDto.TemplateId);
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    #endregion
}
