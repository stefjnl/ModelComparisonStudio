using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Represents a category for organizing prompt templates
/// </summary>
public class PromptCategory
{
    /// <summary>
    /// Unique identifier for the category
    /// </summary>
    [Required]
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Name of the category
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the category
    /// </summary>
    [StringLength(200)]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Color code for the category (hex color)
    /// </summary>
    [StringLength(7)]
    public string Color { get; private set; } = "#6b7280";

    /// <summary>
    /// Timestamp when the category was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Number of templates in this category
    /// </summary>
    public int TemplateCount { get; set; }

    private PromptCategory() { }

    /// <summary>
    /// Creates a new prompt category
    /// </summary>
    public static PromptCategory Create(string name, string? description = null, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        return new PromptCategory
        {
            Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Color = color?.Trim() ?? "#6b7280",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a system category with a fixed ID
    /// </summary>
    public static PromptCategory CreateSystemCategory(string id, string name, string? description = null, string? color = null, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        return new PromptCategory
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Color = color?.Trim() ?? "#6b7280",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the category properties
    /// </summary>
    public void Update(string? name = null, string? description = null, string? color = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        if (description != null)
            Description = description.Trim();

        if (!string.IsNullOrWhiteSpace(color) && IsValidHexColor(color))
            Color = color.Trim();
    }

    /// <summary>
    /// Validates the category
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Category name is required");

        if (Name.Length > 50)
            errors.Add("Category name cannot exceed 50 characters");

        if (Description.Length > 200)
            errors.Add("Description cannot exceed 200 characters");

        if (!IsValidHexColor(Color))
            errors.Add("Color must be a valid hex color code");

        return errors;
    }

    private static bool IsValidHexColor(string color)
    {
        return !string.IsNullOrWhiteSpace(color) && 
               color.Length == 7 && 
               color[0] == '#' && 
               color[1..].All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}

/// <summary>
/// Predefined system categories for prompt templates
/// </summary>
public static class SystemCategories
{
    public static readonly PromptCategory[] DefaultCategories = new[]
    {
        PromptCategory.CreateSystemCategory("creative-writing", "Creative Writing", "Storytelling, poetry, and creative content", "#8b5cf6"),
        PromptCategory.CreateSystemCategory("code-generation", "Code Generation", "Programming code, algorithms, and technical solutions", "#10b981"),
        PromptCategory.CreateSystemCategory("analysis", "Analysis", "Data analysis, reasoning, and problem-solving", "#3b82f6"),
        PromptCategory.CreateSystemCategory("qa", "Q&A", "Question and answer scenarios", "#f59e0b"),
        PromptCategory.CreateSystemCategory("brainstorming", "Brainstorming", "Idea generation and creative thinking", "#ec4899")
    };
}