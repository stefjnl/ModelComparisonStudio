using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Application.DTOs;

/// <summary>
/// Data Transfer Object for prompt categories
/// </summary>
public class PromptCategoryDto
{
    /// <summary>
    /// Unique identifier for the category
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the category
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the category
    /// </summary>
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Color code for the category (hex color)
    /// </summary>
    [StringLength(7)]
    public string Color { get; set; } = "#6b7280";

    /// <summary>
    /// Timestamp when the category was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of templates in this category
    /// </summary>
    public int TemplateCount { get; set; }

    /// <summary>
    /// Converts a domain entity to DTO
    /// </summary>
    public static PromptCategoryDto FromDomainEntity(ModelComparisonStudio.Core.Entities.PromptCategory category)
    {
        return new PromptCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Color = category.Color,
            CreatedAt = category.CreatedAt,
            TemplateCount = category.TemplateCount
        };
    }

    /// <summary>
    /// Converts the DTO to a domain entity
    /// </summary>
    public ModelComparisonStudio.Core.Entities.PromptCategory ToDomainEntity()
    {
        // Use CreateSystemCategory to properly set the Id and CreatedAt while maintaining encapsulation
        // This avoids reflection and follows the existing factory pattern
        return ModelComparisonStudio.Core.Entities.PromptCategory.CreateSystemCategory(
            id: string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString() : Id,
            name: Name,
            description: Description,
            color: Color,
            createdAt: CreatedAt == default ? DateTime.UtcNow : CreatedAt);
    }
}

/// <summary>
/// Data Transfer Object for creating a new category
/// </summary>
public class CreatePromptCategoryDto
{
    /// <summary>
    /// Name of the category
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the category
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Color code for the category (hex color)
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }
}

/// <summary>
/// Data Transfer Object for updating a category
/// </summary>
public class UpdatePromptCategoryDto
{
    /// <summary>
    /// Name of the category
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Description of the category
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Color code for the category (hex color)
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }
}

/// <summary>
/// Data Transfer Object for template statistics
/// </summary>
public class TemplateStatisticsDto
{
    /// <summary>
    /// Total number of templates
    /// </summary>
    public int TotalTemplates { get; set; }

    /// <summary>
    /// Number of system templates
    /// </summary>
    public int SystemTemplates { get; set; }

    /// <summary>
    /// Number of user templates
    /// </summary>
    public int UserTemplates { get; set; }

    /// <summary>
    /// Total number of categories
    /// </summary>
    public int TotalCategories { get; set; }

    /// <summary>
    /// Total usage count across all templates
    /// </summary>
    public int TotalTemplateUsageCount { get; set; }

    /// <summary>
    /// Usage count of the most used template
    /// </summary>
    public int MostUsedTemplateUsageCount { get; set; }

    /// <summary>
    /// Number of templates marked as favorite
    /// </summary>
    public int FavoriteTemplatesCount { get; set; }

    /// <summary>
    /// Date of the last template usage
    /// </summary>
    public DateTime? LastUsedTemplateDate { get; set; }

    /// <summary>
    /// Converts a domain entity to DTO
    /// </summary>
    public static TemplateStatisticsDto FromDomainEntity(ModelComparisonStudio.Core.Interfaces.TemplateStatistics statistics)
    {
        return new TemplateStatisticsDto
        {
            TotalTemplates = statistics.TotalTemplates,
            SystemTemplates = statistics.SystemTemplates,
            UserTemplates = statistics.UserTemplates,
            TotalCategories = statistics.TotalCategories,
            TotalTemplateUsageCount = statistics.TotalTemplateUsageCount,
            MostUsedTemplateUsageCount = statistics.MostUsedTemplateUsageCount,
            FavoriteTemplatesCount = statistics.FavoriteTemplatesCount,
            LastUsedTemplateDate = statistics.LastUsedTemplateDate
        };
    }
}