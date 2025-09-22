using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Represents an AI provider in the domain.
/// </summary>
public class Provider
{
    /// <summary>
    /// Unique identifier for this provider.
    /// </summary>
    [Required]
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Display name of the provider.
    /// </summary>
    [Required]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Base URL for the provider's API.
    /// </summary>
    [Required]
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// List of available model IDs for this provider.
    /// </summary>
    public List<string> AvailableModels { get; private set; } = new();

    /// <summary>
    /// Indicates whether this provider is currently active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Timestamp when this provider was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when this provider was last updated.
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private Provider() { }

    /// <summary>
    /// Creates a new provider instance (for mapping purposes).
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="availableModels">The available models.</param>
    /// <returns>A new provider instance.</returns>
    internal static Provider CreateForMapping(string name, string baseUrl, List<string> availableModels)
    {
        var now = DateTime.UtcNow;
        return new Provider
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            BaseUrl = baseUrl,
            AvailableModels = availableModels.ToList(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Creates a new provider.
    /// </summary>
    /// <param name="name">Display name of the provider.</param>
    /// <param name="baseUrl">Base URL for the provider's API.</param>
    /// <param name="availableModels">List of available model IDs.</param>
    /// <returns>A new provider instance.</returns>
    public static Provider Create(string name, string baseUrl, List<string> availableModels)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name cannot be null or empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));
        }

        if (availableModels == null)
        {
            throw new ArgumentNullException(nameof(availableModels));
        }

        var now = DateTime.UtcNow;
        return new Provider
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            BaseUrl = baseUrl,
            AvailableModels = availableModels.ToList(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Updates the provider's information.
    /// </summary>
    /// <param name="name">New display name (optional).</param>
    /// <param name="baseUrl">New base URL (optional).</param>
    /// <param name="availableModels">New list of available models (optional).</param>
    /// <param name="isActive">New active status (optional).</param>
    public void Update(
        string? name = null,
        string? baseUrl = null,
        List<string>? availableModels = null,
        bool? isActive = null)
    {
        if (name != null && !string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        if (baseUrl != null && !string.IsNullOrWhiteSpace(baseUrl))
        {
            BaseUrl = baseUrl;
        }

        if (availableModels != null)
        {
            AvailableModels = availableModels.ToList();
        }

        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates this provider.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this provider.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a specific model is available for this provider.
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <returns>True if the model is available, false otherwise.</returns>
    public bool IsModelAvailable(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        return AvailableModels.Any(model =>
            model.Equals(modelId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the count of available models.
    /// </summary>
    /// <returns>The number of available models.</returns>
    public int GetModelCount()
    {
        return AvailableModels.Count;
    }

    /// <summary>
    /// Gets a model by ID (case-insensitive).
    /// </summary>
    /// <param name="modelId">The model ID to find.</param>
    /// <returns>The model ID if found, null otherwise.</returns>
    public string? GetModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return null;
        }

        return AvailableModels.FirstOrDefault(model =>
            model.Equals(modelId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a new model to this provider.
    /// </summary>
    /// <param name="modelId">The model ID to add.</param>
    public void AddModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        if (!AvailableModels.Contains(modelId, StringComparer.OrdinalIgnoreCase))
        {
            AvailableModels.Add(modelId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes a model from this provider.
    /// </summary>
    /// <param name="modelId">The model ID to remove.</param>
    /// <returns>True if the model was removed, false if it wasn't found.</returns>
    public bool RemoveModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var modelToRemove = AvailableModels.FirstOrDefault(model =>
            model.Equals(modelId, StringComparison.OrdinalIgnoreCase));

        if (modelToRemove != null)
        {
            AvailableModels.Remove(modelToRemove);
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }
}
