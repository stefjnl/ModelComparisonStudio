using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using ModelComparisonStudio.Core.Entities;
using ModelComparisonStudio.Core.Interfaces;

namespace ModelComparisonStudio.Application.Services;

/// <summary>
/// Service for prompt category management operations
/// </summary>
public class PromptCategoryService
{
    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<PromptCategoryService> _logger;

    public PromptCategoryService(
        IPromptTemplateRepository repository,
        ILogger<PromptCategoryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    public async Task<IEnumerable<PromptCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all categories");
        return await _repository.GetAllCategoriesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a category by its ID
    /// </summary>
    public async Task<PromptCategory?> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Category ID cannot be null or empty", nameof(id));

        _logger.LogInformation("Getting category by ID: {CategoryId}", id);
        return await _repository.GetCategoryByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Creates a new category
    /// </summary>
    public async Task<PromptCategory> CreateCategoryAsync(
        string name,
        string? description = null,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        var category = PromptCategory.Create(name, description, color);

        // Validate the category
        var validationErrors = category.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = $"Category validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogWarning(errorMessage);
            throw new ValidationException(errorMessage);
        }

        var success = await _repository.AddCategoryAsync(category, cancellationToken);
        if (!success)
            throw new Exception("Failed to create category");

        _logger.LogInformation("Created category: {CategoryId} ({Name})", category.Id, category.Name);
        return category;
    }

    /// <summary>
    /// Updates a category
    /// </summary>
    public async Task<PromptCategory> UpdateCategoryAsync(
        string id,
        string? name = null,
        string? description = null,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Category ID cannot be null or empty", nameof(id));

        var category = await _repository.GetCategoryByIdAsync(id, cancellationToken);
        if (category == null)
            throw new ArgumentException($"Category with ID {id} does not exist", nameof(id));

        category.Update(name: name, description: description, color: color);

        // Validate the category
        var validationErrors = category.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = $"Category validation failed: {string.Join(", ", validationErrors)}";
            _logger.LogWarning(errorMessage);
            throw new ValidationException(errorMessage);
        }

        var success = await _repository.UpdateCategoryAsync(category, cancellationToken);
        if (!success)
            throw new Exception("Failed to update category");

        _logger.LogInformation("Updated category: {CategoryId} ({Name})", category.Id, category.Name);
        return category;
    }

    /// <summary>
    /// Deletes a category
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Category ID cannot be null or empty", nameof(id));

        var success = await _repository.DeleteCategoryAsync(id, cancellationToken);
        if (success)
        {
            _logger.LogInformation("Deleted category: {CategoryId}", id);
        }
        else
        {
            _logger.LogWarning("Failed to delete category: {CategoryId}", id);
        }

        return success;
    }
}