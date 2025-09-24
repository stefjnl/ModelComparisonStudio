using Microsoft.Extensions.Logging;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Application.Services;

/// <summary>
/// Application service for template statistics operations
/// </summary>
public class TemplateStatisticsService
{
    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<TemplateStatisticsService> _logger;

    public TemplateStatisticsService(
        IPromptTemplateRepository repository,
        ILogger<TemplateStatisticsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets template statistics
    /// </summary>
    public async Task<TemplateStatistics> GetTemplateStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting template statistics");
        return await _repository.GetTemplateStatisticsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets most used templates
    /// </summary>
    public async Task<IEnumerable<PromptTemplate>> GetMostUsedTemplatesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} most used templates", limit);
        return await _repository.GetMostUsedTemplatesAsync(limit, cancellationToken);
    }

    /// <summary>
    /// Gets most used templates with categories pre-loaded
    /// </summary>
    public async Task<(IEnumerable<PromptTemplate> Templates, IEnumerable<PromptCategory> Categories)> GetMostUsedTemplatesWithCategoriesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} most used templates with categories", limit);

        var templates = await _repository.GetMostUsedTemplatesAsync(limit, cancellationToken);
        var categories = await _repository.GetAllCategoriesAsync(cancellationToken);

        return (templates, categories);
    }

    /// <summary>
    /// Gets recently used templates
    /// </summary>
    public async Task<IEnumerable<PromptTemplate>> GetRecentTemplatesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} recently used templates", limit);
        return await _repository.GetRecentTemplatesAsync(limit, cancellationToken);
    }

    /// <summary>
    /// Gets recently used templates with categories pre-loaded
    /// </summary>
    public async Task<(IEnumerable<PromptTemplate> Templates, IEnumerable<PromptCategory> Categories)> GetRecentTemplatesWithCategoriesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {Limit} recently used templates with categories", limit);

        var templates = await _repository.GetRecentTemplatesAsync(limit, cancellationToken);
        var categories = await _repository.GetAllCategoriesAsync(cancellationToken);

        return (templates, categories);
    }
}