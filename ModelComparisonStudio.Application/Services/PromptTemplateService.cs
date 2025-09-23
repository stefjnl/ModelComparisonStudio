using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Application.Services;

/// <summary>
/// Service for prompt template management operations
/// </summary>
public class PromptTemplateService
{
    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<PromptTemplateService> _logger;

    public PromptTemplateService(
        IPromptTemplateRepository repository,
        ILogger<PromptTemplateService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all available templates
    /// </summary>
    public async Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all prompt templates");
        return await _repository.GetAllTemplatesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all templates with their categories pre-loaded to avoid N+1 queries
    /// </summary>
    public async Task<(IEnumerable<PromptTemplate> Templates, IEnumerable<PromptCategory> Categories)> GetAllTemplatesWithCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all prompt templates with categories");

        var templates = await _repository.GetAllTemplatesAsync(cancellationToken);
        var categories = await _repository.GetAllCategoriesAsync(cancellationToken);

        return (templates, categories);
    }

    /// <summary>
    /// Gets a template by its ID
    /// </summary>
    public async Task<PromptTemplate?> GetTemplateByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Template ID cannot be null or empty", nameof(id));

        _logger.LogInformation("Getting template by ID: {TemplateId}", id);
        return await _repository.GetTemplateByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets templates by category
    /// </summary>
    public async Task<IEnumerable<PromptTemplate>> GetTemplatesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category ID cannot be null or empty", nameof(categoryId));

        _logger.LogInformation("Getting templates by category: {CategoryId}", categoryId);
        return await _repository.GetTemplatesByCategoryAsync(categoryId, cancellationToken);
    }

    /// <summary>
    /// Gets templates by category with categories pre-loaded
    /// </summary>
    public async Task<(IEnumerable<PromptTemplate> Templates, IEnumerable<PromptCategory> Categories)> GetTemplatesByCategoryWithCategoriesAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category ID cannot be null or empty", nameof(categoryId));

        _logger.LogInformation("Getting templates by category with categories: {CategoryId}", categoryId);

        var templates = await _repository.GetTemplatesByCategoryAsync(categoryId, cancellationToken);
        var categories = await _repository.GetAllCategoriesAsync(cancellationToken);

        return (templates, categories);
    }

    /// <summary>
    /// Searches templates by name or content
    /// </summary>
    public async Task<IEnumerable<PromptTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching templates with term: {SearchTerm}", searchTerm);
        return await _repository.SearchTemplatesAsync(searchTerm, cancellationToken);
    }

    /// <summary>
    /// Searches templates by name or content with categories pre-loaded
    /// </summary>
    public async Task<(IEnumerable<PromptTemplate> Templates, IEnumerable<PromptCategory> Categories)> SearchTemplatesWithCategoriesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching templates with term and categories: {SearchTerm}", searchTerm);

        var templates = await _repository.SearchTemplatesAsync(searchTerm, cancellationToken);
        var categories = await _repository.GetAllCategoriesAsync(cancellationToken);

        return (templates, categories);
    }

    /// <summary>
    /// Gets favorite templates
    /// </summary>
    public async Task<IEnumerable<PromptTemplate>> GetFavoriteTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting favorite templates");
        return await _repository.GetFavoriteTemplatesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets favorite templates with categories pre-loaded
    /// </summary>
    public async Task<(IEnumerable<PromptTemplate> Templates, IEnumerable<PromptCategory> Categories)> GetFavoriteTemplatesWithCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting favorite templates with categories");

        var templates = await _repository.GetFavoriteTemplatesAsync(cancellationToken);
        var categories = await _repository.GetAllCategoriesAsync(cancellationToken);

        return (templates, categories);
    }

    /// <summary>
    /// Creates a new template
    /// </summary>
    public async Task<PromptTemplate> CreateTemplateAsync(
        string title,
        string description,
        string content,
        string categoryId,
        List<TemplateVariable>? variables = null,
        bool isSystemTemplate = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category ID cannot be null or empty", nameof(categoryId));

        // Validate the category exists
        var category = await _repository.GetCategoryByIdAsync(categoryId, cancellationToken);
        if (category == null)
            throw new ArgumentException($"Category with ID {categoryId} does not exist", nameof(categoryId));

        var template = PromptTemplate.Create(
            title: title,
            description: description,
            content: content,
            category: categoryId,
            variables: variables,
            isSystemTemplate: isSystemTemplate);

        // Validate the template
        var validationErrors = template.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = $"Template validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogWarning(errorMessage);
            throw new ValidationException(errorMessage);
        }

        var success = await _repository.AddTemplateAsync(template, cancellationToken);
        if (!success)
            throw new Exception("Failed to create template");

        _logger.LogInformation("Created template: {TemplateId} ({Title})", template.Id, template.Title);
        return template;
    }

    /// <summary>
    /// Updates an existing template
    /// </summary>
    public async Task<PromptTemplate> UpdateTemplateAsync(
        string id,
        string? title = null,
        string? description = null,
        string? content = null,
        string? categoryId = null,
        List<TemplateVariable>? variables = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Template ID cannot be null or empty", nameof(id));

        var template = await _repository.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
            throw new ArgumentException($"Template with ID {id} does not exist", nameof(id));

        // Validate category if provided
        if (categoryId != null)
        {
            var category = await _repository.GetCategoryByIdAsync(categoryId, cancellationToken);
            if (category == null)
                throw new ArgumentException($"Category with ID {categoryId} does not exist", nameof(categoryId));
        }

        template.Update(
            title: title,
            description: description,
            content: content,
            category: categoryId,
            variables: variables);

        // Validate the template
        var validationErrors = template.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = $"Template validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogWarning(errorMessage);
            throw new ValidationException(errorMessage);
        }

        var success = await _repository.UpdateTemplateAsync(template, cancellationToken);
        if (!success)
            throw new Exception("Failed to update template");

        _logger.LogInformation("Updated template: {TemplateId} ({Title})", template.Id, template.Title);
        return template;
    }

    /// <summary>
    /// Deletes a template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Template ID cannot be null or empty", nameof(id));

        var success = await _repository.DeleteTemplateAsync(id, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Deleted template: {TemplateId}", id);
        }
        else
        {
            _logger.LogWarning("Failed to delete template: {TemplateId}", id);
        }

        return success;
    }

    /// <summary>
    /// Increments the usage count for a template
    /// </summary>
    public async Task<bool> IncrementTemplateUsageAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Template ID cannot be null or empty", nameof(id));

        var success = await _repository.IncrementTemplateUsageAsync(id, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Incremented usage count for template: {TemplateId}", id);
        }
        else
        {
            _logger.LogWarning("Failed to increment usage count for template: {TemplateId}", id);
        }

        return success;
    }

    /// <summary>
    /// Toggles the favorite status of a template
    /// </summary>
    public async Task<bool> ToggleTemplateFavoriteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Template ID cannot be null or empty", nameof(id));

        var success = await _repository.ToggleTemplateFavoriteAsync(id, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Toggled favorite status for template: {TemplateId}", id);
        }
        else
        {
            _logger.LogWarning("Failed to toggle favorite status for template: {TemplateId}", id);
        }

        return success;
    }

    /// <summary>
    /// Expands a template with variable values
    /// </summary>
    public async Task<string> ExpandTemplateAsync(string id, Dictionary<string, string> variableValues, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Template ID cannot be null or empty", nameof(id));

        var template = await _repository.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
            throw new ArgumentException($"Template with ID {id} does not exist", nameof(id));

        // Validate required variables
        var missingRequiredVariables = template.Variables
            .Where(v => v.IsRequired && !variableValues.ContainsKey(v.Name))
            .Select(v => v.Name)
            .ToList();

        if (missingRequiredVariables.Any())
        {
            var errorMessage = $"Missing required variables: {string.Join(", ", missingRequiredVariables)}";
            _logger.LogWarning(errorMessage);
            throw new ValidationException(errorMessage);
        }

        // Expand the template
        var expandedContent = template.ExpandTemplate(variableValues);

        // Increment usage count
        await IncrementTemplateUsageAsync(id, cancellationToken);

        _logger.LogInformation("Expanded template: {TemplateId} ({Title})", template.Id, template.Title);
        return expandedContent;
    }
}