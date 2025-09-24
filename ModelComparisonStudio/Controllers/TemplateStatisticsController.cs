using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Application.Services;
using ModelComparisonStudio.Application.DTOs;

namespace ModelComparisonStudio.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public class TemplateStatisticsController : BaseController
{
    private readonly TemplateStatisticsService _statisticsService;

    public TemplateStatisticsController(
        TemplateStatisticsService statisticsService,
        ILogger<TemplateStatisticsController> logger) : base(logger)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    /// <summary>
    /// Gets template statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TemplateStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _statisticsService.GetTemplateStatisticsAsync(cancellationToken);
            var dto = TemplateStatisticsDto.FromDomainEntity(statistics);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template statistics");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Gets most used templates
    /// </summary>
    [HttpGet("statistics/most-used")]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMostUsedTemplates([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var (templates, categories) = await _statisticsService.GetMostUsedTemplatesWithCategoriesAsync(limit, cancellationToken);

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
            _logger.LogError(ex, "Error getting most used templates");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    /// <summary>
    /// Gets recently used templates
    /// </summary>
    [HttpGet("statistics/recently-used")]
    [ProducesResponseType(typeof(IEnumerable<PromptTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecentlyUsedTemplates([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var (templates, categories) = await _statisticsService.GetRecentTemplatesWithCategoriesAsync(limit, cancellationToken);

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
            _logger.LogError(ex, "Error getting recently used templates");
            return StatusCode(500, CreateErrorResponse(ex));
        }
    }

    }