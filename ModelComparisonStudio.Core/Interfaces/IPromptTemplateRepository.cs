using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ModelComparisonStudio.Core.Entities;

namespace ModelComparisonStudio.Core.Interfaces;

/// <summary>
/// Repository for managing prompt templates and categories
/// </summary>
public interface IPromptTemplateRepository
{
    #region Prompt Template Operations
    
    /// <summary>
    /// Gets a template by its ID
    /// </summary>
    Task<PromptTemplate?> GetTemplateByIdAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all templates
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all templates with categories included
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetAllTemplatesWithCategoriesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets templates by category
    /// <summary>
    /// Gets templates by category with categories included
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetTemplatesByCategoryWithCategoriesAsync(string categoryId, CancellationToken cancellationToken = default);
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetTemplatesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets templates by type (system vs user)
    /// <summary>
    /// Searches templates with categories included
    /// </summary>
    Task<IEnumerable<PromptTemplate>> SearchTemplatesWithCategoriesAsync(string searchTerm, CancellationToken cancellationToken = default);
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetTemplatesByTypeAsync(bool isSystemTemplate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets favorite templates with categories included
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetFavoriteTemplatesWithCategoriesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Searches templates by name or content
    /// </summary>
    Task<IEnumerable<PromptTemplate>> SearchTemplatesAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets favorite templates
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetFavoriteTemplatesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new template
    /// </summary>
    Task<bool> AddTemplateAsync(PromptTemplate template, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing template
    /// </summary>
    Task<bool> UpdateTemplateAsync(PromptTemplate template, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a template
    /// </summary>
    Task<bool> DeleteTemplateAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increments the usage count for a template
    /// </summary>
    Task<bool> IncrementTemplateUsageAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Toggles the favorite status of a template
    /// </summary>
    Task<bool> ToggleTemplateFavoriteAsync(string id, CancellationToken cancellationToken = default);

    #endregion
    
    #region Prompt Category Operations
    
    /// <summary>
    /// Gets a category by its ID
    /// </summary>
    Task<PromptCategory?> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all categories
    /// </summary>
    Task<IEnumerable<PromptCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the default system categories
    /// </summary>
    Task<IEnumerable<PromptCategory>> GetSystemCategoriesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new category
    /// </summary>
    Task<bool> AddCategoryAsync(PromptCategory category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing category
    /// </summary>
    Task<bool> UpdateCategoryAsync(PromptCategory category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a category
    /// </summary>
    Task<bool> DeleteCategoryAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the template count for a category
    /// </summary>
    Task<bool> UpdateCategoryTemplateCountAsync(string categoryId, CancellationToken cancellationToken = default);

    #endregion
    
    #region Statistics
    
    /// <summary>
    /// Gets template statistics (total templates, usage counts, etc.)
    /// </summary>
    Task<TemplateStatistics> GetTemplateStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the most used templates
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetMostUsedTemplatesAsync(int limit = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets recently used templates
    /// </summary>
    Task<IEnumerable<PromptTemplate>> GetRecentTemplatesAsync(int limit = 10, CancellationToken cancellationToken = default);

    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initializes the database with default system categories
    /// </summary>
    Task<bool> InitializeDatabaseAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Statistics for prompt templates
/// </summary>
public class TemplateStatistics
{
    public int TotalTemplates { get; set; }
    public int SystemTemplates { get; set; }
    public int UserTemplates { get; set; }
    public int TotalCategories { get; set; }
    public int TotalTemplateUsageCount { get; set; }
    public int MostUsedTemplateUsageCount { get; set; }
    public int FavoriteTemplatesCount { get; set; }
    public DateTime? LastUsedTemplateDate { get; set; }
}