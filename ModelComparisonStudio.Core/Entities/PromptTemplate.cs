using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ModelComparisonStudio.Core.ValueObjects;

namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Types of templates
/// </summary>
public enum TemplateType
{
    /// <summary>
    /// System template (cannot be edited or deleted)
    /// </summary>
    System = 0,
    
    /// <summary>
    /// User-created template
    /// </summary>
    User = 1
}

/// <summary>
/// Extension methods for template variables
/// </summary>
public static class TemplateVariableExtensions
{
    /// <summary>
    /// Serializes the template variable to JSON
    /// </summary>
    public static string ToJson(this List<TemplateVariable> variables)
    {
        return JsonSerializer.Serialize(variables, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Deserializes template variables from JSON
    /// </summary>
    public static List<TemplateVariable> FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<TemplateVariable>();

        try
        {
            return JsonSerializer.Deserialize<List<TemplateVariable>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new List<TemplateVariable>();
        }
        catch (JsonException)
        {
            return new List<TemplateVariable>();
        }
    }
}

/// <summary>
/// Represents a prompt template that can be reused for model comparisons
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    [Required]
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Title of the template
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the template
    /// </summary>
    [StringLength(500)]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The actual template content with variables
    /// </summary>
    [Required]
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Category identifier for the template
    /// </summary>
    [Required]
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// JSON serialized list of template variables
    /// </summary>
    public string VariablesJson { get; private set; } = "[]";

    /// <summary>
    /// List of template variables (deserialized from JSON)
    /// </summary>
    public List<TemplateVariable> Variables => 
        JsonSerializer.Deserialize<List<TemplateVariable>>(VariablesJson) ?? new List<TemplateVariable>();

    /// <summary>
    /// Number of times this template has been used
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Whether this template is marked as favorite
    /// </summary>
    public bool IsFavorite { get; private set; }

    /// <summary>
    /// Whether this is a system template (cannot be edited/deleted)
    /// </summary>
    public bool IsSystemTemplate { get; private set; }

    /// <summary>
    /// Timestamp when the template was last used
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Timestamp when the template was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the template was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; private set; }

    private PromptTemplate() { }

    /// <summary>
    /// Creates a new prompt template
    /// </summary>
    public static PromptTemplate Create(
        string title, 
        string description, 
        string content, 
        string category,
        List<TemplateVariable>? variables = null,
        bool isSystemTemplate = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty", nameof(title));
        
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));
        
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));

        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Content = content,
            Category = category,
            VariablesJson = JsonSerializer.Serialize(variables ?? new List<TemplateVariable>()),
            IsSystemTemplate = isSystemTemplate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return template;
    }

    /// <summary>
    /// Updates the template properties
    /// </summary>
    public void Update(
        string? title = null,
        string? description = null,
        string? content = null,
        string? category = null,
        List<TemplateVariable>? variables = null)
    {
        if (IsSystemTemplate)
            throw new InvalidOperationException("Cannot modify system templates");

        if (!string.IsNullOrWhiteSpace(title))
            Title = title.Trim();

        if (description != null)
            Description = description.Trim();

        if (!string.IsNullOrWhiteSpace(content))
            Content = content;

        if (!string.IsNullOrWhiteSpace(category))
            Category = category;

        if (variables != null)
            VariablesJson = JsonSerializer.Serialize(variables);

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the usage count
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Toggles the favorite status
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the template
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required");

        if (string.IsNullOrWhiteSpace(Content))
            errors.Add("Content is required");

        if (string.IsNullOrWhiteSpace(Category))
            errors.Add("Category is required");

        if (Title.Length > 100)
            errors.Add("Title cannot exceed 100 characters");

        if (Description.Length > 500)
            errors.Add("Description cannot exceed 500 characters");

        // Validate variables
        try
        {
            var variables = Variables;
            foreach (var variable in variables)
            {
                var variableErrors = variable.Validate();
                errors.AddRange(variableErrors);
            }
        }
        catch (JsonException)
        {
            errors.Add("Invalid variables JSON format");
        }

        return errors;
    }

    /// <summary>
    /// Expands the template with variable values
    /// </summary>
    public string ExpandTemplate(Dictionary<string, string> variableValues)
    {
        var expandedContent = Content;
        
        foreach (var variable in Variables)
        {
            var placeholder = $"{{{{{variable.Name}}}}}";
            var value = variableValues.GetValueOrDefault(variable.Name, variable.DefaultValue ?? "");
            expandedContent = expandedContent.Replace(placeholder, value);
        }

        return expandedContent;
    }
}

/// <summary>
/// Represents a variable in a prompt template
/// </summary>
public class TemplateVariable
{
    /// <summary>
    /// Name of the variable (without braces)
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the variable
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Default value for the variable
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether the variable is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Validates the variable
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Variable name is required");

        if (!IsValidVariableName(Name))
            errors.Add($"Invalid variable name: '{Name}'. Must start with a letter and contain only letters, numbers, and underscores");

        return errors;
    }

    private static bool IsValidVariableName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               char.IsLetter(name[0]) &&
               name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

}