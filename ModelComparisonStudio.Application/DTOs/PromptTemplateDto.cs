using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ModelComparisonStudio.Core.Entities;

namespace ModelComparisonStudio.Application.DTOs;

/// <summary>
/// Data Transfer Object for prompt templates
/// </summary>
public class PromptTemplateDto
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Title of the template
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the template
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The actual template content with variables
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Category identifier for the template
    /// </summary>
    [Required]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Name of the category (for display)
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Color of the category (for display)
    /// </summary>
    public string? CategoryColor { get; set; }

    /// <summary>
    /// List of template variables
    /// </summary>
    public List<TemplateVariableDto> Variables { get; set; } = new();

    /// <summary>
    /// Number of times this template has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether this template is marked as favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Whether this is a system template (cannot be edited/deleted)
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Timestamp when the template was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the template was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when the template was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Converts a domain entity to DTO
    /// </summary>
    public static PromptTemplateDto FromDomainEntity(PromptTemplate template, PromptCategory? category = null)
    {
        return new PromptTemplateDto
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            Content = template.Content,
            Category = template.Category,
            CategoryName = category?.Name,
            CategoryColor = category?.Color,
            Variables = template.Variables.Select(v => TemplateVariableDto.FromDomainEntity(v)).ToList(),
            UsageCount = template.UsageCount,
            IsFavorite = template.IsFavorite,
            IsSystemTemplate = template.IsSystemTemplate,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            LastUsedAt = template.LastUsedAt
        };
    }

    /// <summary>
    /// Converts the DTO to a domain entity
    /// </summary>
    public PromptTemplate ToDomainEntity()
    {
        return PromptTemplate.Create(
            title: Title,
            description: Description,
            content: Content,
            category: Category,
            variables: Variables.Select(v => v.ToDomainEntity()).ToList(),
            isSystemTemplate: IsSystemTemplate);
    }
}

/// <summary>
/// Data Transfer Object for creating a new prompt template
/// </summary>
public class CreatePromptTemplateDto
{
    /// <summary>
    /// Title of the template
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the template
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The actual template content with variables
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Category identifier for the template
    /// </summary>
    [Required]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// List of template variables
    /// </summary>
    public List<TemplateVariableDto>? Variables { get; set; }

    /// <summary>
    /// Whether this is a system template
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;
}

/// <summary>
/// Data Transfer Object for updating a prompt template
/// </summary>
public class UpdatePromptTemplateDto
{
    /// <summary>
    /// Title of the template
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Title { get; set; }

    /// <summary>
    /// Description of the template
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The actual template content with variables
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Category identifier for the template
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// List of template variables
    /// </summary>
    public List<TemplateVariableDto>? Variables { get; set; }
}

/// <summary>
/// Data Transfer Object for template variable
/// </summary>
public class TemplateVariableDto
{
    /// <summary>
    /// Name of the variable (without braces)
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the variable
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default value for the variable
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether the variable is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Converts a domain entity to DTO
    /// </summary>
    public static TemplateVariableDto FromDomainEntity(TemplateVariable variable)
    {
        return new TemplateVariableDto
        {
            Name = variable.Name,
            Description = variable.Description,
            DefaultValue = variable.DefaultValue,
            IsRequired = variable.IsRequired
        };
    }

    /// <summary>
    /// Converts the DTO to a domain entity
    /// </summary>
    public TemplateVariable ToDomainEntity()
    {
        return new TemplateVariable
        {
            Name = Name,
            Description = Description ?? string.Empty,
            DefaultValue = DefaultValue,
            IsRequired = IsRequired
        };
    }
}

/// <summary>
/// Data Transfer Object for template expansion
/// </summary>
public class ExpandTemplateDto
{
    /// <summary>
    /// Template ID to expand
    /// </summary>
    [Required]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Variable values for substitution
    /// </summary>
    [Required]
    public Dictionary<string, string> VariableValues { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for template expansion response
/// </summary>
public class ExpandedTemplateDto
{
    /// <summary>
    /// Expanded content
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Template ID
    /// </summary>
    [Required]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Template title
    /// </summary>
    [Required]
    public string TemplateTitle { get; set; } = string.Empty;
}