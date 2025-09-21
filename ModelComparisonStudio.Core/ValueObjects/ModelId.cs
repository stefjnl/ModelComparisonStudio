using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ModelComparisonStudio.Core.ValueObjects;

/// <summary>
/// Represents a model identifier for AI models.
/// </summary>
public class ModelId
{
    /// <summary>
    /// The model ID string.
    /// </summary>
    [Required]
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// The base model name (without provider prefix).
    /// </summary>
    public string BaseModel => ExtractBaseModel(Value);

    /// <summary>
    /// The provider name (if present in the model ID).
    /// </summary>
    public string? Provider => ExtractProvider(Value);

    /// <summary>
    /// Indicates if this model ID includes a provider prefix.
    /// </summary>
    public bool HasProviderPrefix => Provider != null;

    /// <summary>
    /// Indicates if this is a free model (contains ":free" suffix).
    /// </summary>
    public bool IsFreeModel => Value.Contains(":free", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private ModelId() { }

    /// <summary>
    /// Creates a new model ID with the specified value.
    /// </summary>
    /// <param name="value">The model ID value.</param>
    /// <returns>A new model ID instance.</returns>
    public static ModelId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(value));
        }

        var trimmedValue = value.Trim();
        ValidateModelId(trimmedValue);

        return new ModelId
        {
            Value = trimmedValue
        };
    }

    /// <summary>
    /// Creates a model ID from a string without validation (for internal use).
    /// </summary>
    /// <param name="value">The model ID value.</param>
    /// <returns>A new model ID instance.</returns>
    internal static ModelId FromString(string value)
    {
        return new ModelId
        {
            Value = value
        };
    }

    /// <summary>
    /// Validates the model ID format.
    /// </summary>
    /// <param name="modelId">The model ID to validate.</param>
    private static void ValidateModelId(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or empty.", nameof(modelId));
        }

        // Basic validation - model IDs should contain only allowed characters
        var validPattern = @"^[a-zA-Z0-9_\-\.\/:]+$";
        if (!Regex.IsMatch(modelId, validPattern))
        {
            throw new ArgumentException("Model ID contains invalid characters.", nameof(modelId));
        }

        // Check for reasonable length
        if (modelId.Length > 200)
        {
            throw new ArgumentException("Model ID is too long (maximum 200 characters).", nameof(modelId));
        }

        // Check for minimum length
        if (modelId.Length < 2)
        {
            throw new ArgumentException("Model ID is too short (minimum 2 characters).", nameof(modelId));
        }
    }

    /// <summary>
    /// Extracts the provider name from a model ID.
    /// </summary>
    /// <param name="modelId">The model ID to extract from.</param>
    /// <returns>The provider name, or null if not present.</returns>
    private static string? ExtractProvider(string modelId)
    {
        // Common patterns: "provider/model-name" or "provider:model-name"
        var slashIndex = modelId.IndexOf('/');
        var colonIndex = modelId.IndexOf(':');

        if (slashIndex > 0)
        {
            return modelId.Substring(0, slashIndex);
        }

        if (colonIndex > 0)
        {
            return modelId.Substring(0, colonIndex);
        }

        return null;
    }

    /// <summary>
    /// Extracts the base model name from a model ID.
    /// </summary>
    /// <param name="modelId">The model ID to extract from.</param>
    /// <returns>The base model name.</returns>
    private static string ExtractBaseModel(string modelId)
    {
        var slashIndex = modelId.IndexOf('/');
        var colonIndex = modelId.IndexOf(':');

        if (slashIndex > 0)
        {
            return modelId.Substring(slashIndex + 1);
        }

        if (colonIndex > 0)
        {
            return modelId.Substring(colonIndex + 1);
        }

        return modelId;
    }

    /// <summary>
    /// Gets the model ID without the ":free" suffix if present.
    /// </summary>
    /// <returns>The model ID without free suffix.</returns>
    public string GetModelIdWithoutFreeSuffix()
    {
        if (IsFreeModel)
        {
            return Value.Replace(":free", "", StringComparison.OrdinalIgnoreCase);
        }

        return Value;
    }

    /// <summary>
    /// Gets the model ID with the ":free" suffix.
    /// </summary>
    /// <returns>The model ID with free suffix.</returns>
    public string GetModelIdWithFreeSuffix()
    {
        if (!IsFreeModel)
        {
            return Value + ":free";
        }

        return Value;
    }

    /// <summary>
    /// Checks if this model ID matches another model ID (case-insensitive).
    /// </summary>
    /// <param name="other">The other model ID to compare with.</param>
    /// <returns>True if the model IDs match, false otherwise.</returns>
    public bool Matches(ModelId other)
    {
        if (other == null)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this model ID matches a string value (case-insensitive).
    /// </summary>
    /// <param name="modelId">The model ID string to compare with.</param>
    /// <returns>True if the model IDs match, false otherwise.</returns>
    public bool Matches(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        return string.Equals(Value, modelId.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this model ID starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to check for.</param>
    /// <returns>True if the model ID starts with the prefix, false otherwise.</returns>
    public bool StartsWith(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        return Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this model ID contains the specified substring.
    /// </summary>
    /// <param name="substring">The substring to check for.</param>
    /// <returns>True if the model ID contains the substring, false otherwise.</returns>
    public bool Contains(string substring)
    {
        if (string.IsNullOrWhiteSpace(substring))
        {
            return false;
        }

        return Value.Contains(substring, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the string representation of the model ID.
    /// </summary>
    /// <returns>The model ID value.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>True if the specified object is equal to the current object, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not ModelId other)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return Value.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// Determines whether two model IDs are equal.
    /// </summary>
    /// <param name="left">The first model ID to compare.</param>
    /// <param name="right">The second model ID to compare.</param>
    /// <returns>True if the model IDs are equal, false otherwise.</returns>
    public static bool operator ==(ModelId? left, ModelId? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two model IDs are not equal.
    /// </summary>
    /// <param name="left">The first model ID to compare.</param>
    /// <param name="right">The second model ID to compare.</param>
    /// <returns>True if the model IDs are not equal, false otherwise.</returns>
    public static bool operator !=(ModelId? left, ModelId? right)
    {
        return !(left == right);
    }
}
