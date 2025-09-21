using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ModelComparisonStudio.Core.ValueObjects;

/// <summary>
/// Represents an API key for external services.
/// </summary>
public class ApiKey
{
    /// <summary>
    /// The API key value.
    /// </summary>
    [Required]
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// The length of the API key.
    /// </summary>
    public int Length => Value.Length;

    /// <summary>
    /// Indicates if this API key is empty or null.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Indicates if this API key appears to be valid (basic validation).
    /// </summary>
    public bool IsValid => ValidateApiKeyFormat(Value);

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private ApiKey() { }

    /// <summary>
    /// Creates a new API key with the specified value.
    /// </summary>
    /// <param name="value">The API key value.</param>
    /// <returns>A new API key instance.</returns>
    public static ApiKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("API key cannot be null or empty.", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (!ValidateApiKeyFormat(trimmedValue))
        {
            throw new ArgumentException("API key format is invalid.", nameof(value));
        }

        return new ApiKey
        {
            Value = trimmedValue
        };
    }

    /// <summary>
    /// Creates an empty API key.
    /// </summary>
    /// <returns>A new empty API key instance.</returns>
    public static ApiKey CreateEmpty()
    {
        return new ApiKey
        {
            Value = string.Empty
        };
    }

    /// <summary>
    /// Creates an API key from a string without validation (for internal use).
    /// </summary>
    /// <param name="value">The API key value.</param>
    /// <returns>A new API key instance.</returns>
    internal static ApiKey FromString(string value)
    {
        return new ApiKey
        {
            Value = value
        };
    }

    /// <summary>
    /// Validates the API key format.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <returns>True if the API key format is valid, false otherwise.</returns>
    private static bool ValidateApiKeyFormat(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        // API keys should be at least 10 characters long
        if (apiKey.Length < 10)
        {
            return false;
        }

        // API keys should not exceed 200 characters
        if (apiKey.Length > 200)
        {
            return false;
        }

        // API keys should contain only allowed characters
        // Allow alphanumeric characters, hyphens, underscores, and common special characters
        var validPattern = @"^[a-zA-Z0-9_\-\.\/\+=]+$";
        if (!Regex.IsMatch(apiKey, validPattern))
        {
            return false;
        }

        // API keys should not contain common placeholder text
        var placeholderTexts = new[] { "your", "api", "key", "here", "placeholder", "example", "test" };
        var lowerApiKey = apiKey.ToLowerInvariant();

        if (placeholderTexts.Any(placeholder => lowerApiKey.Contains(placeholder)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a masked version of the API key for display purposes.
    /// </summary>
    /// <param name="visibleLength">Number of characters to show at the beginning and end.</param>
    /// <returns>A masked version of the API key.</returns>
    public string GetMasked(int visibleLength = 4)
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        if (Length <= visibleLength * 2)
        {
            return new string('*', Length);
        }

        var start = Value.Substring(0, visibleLength);
        var end = Value.Substring(Length - visibleLength, visibleLength);
        var maskLength = Length - (visibleLength * 2);

        return $"{start}{new string('*', maskLength)}{end}";
    }

    /// <summary>
    /// Gets a partially masked version of the API key.
    /// </summary>
    /// <param name="visibleStartLength">Number of characters to show at the beginning.</param>
    /// <returns>A partially masked version of the API key.</returns>
    public string GetPartialMask(int visibleStartLength = 8)
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        if (Length <= visibleStartLength)
        {
            return new string('*', Length);
        }

        var start = Value.Substring(0, visibleStartLength);
        var maskLength = Length - visibleStartLength;

        return $"{start}{new string('*', maskLength)}";
    }

    /// <summary>
    /// Gets the API key prefix (first few characters).
    /// </summary>
    /// <param name="length">Number of characters to include in the prefix.</param>
    /// <returns>The API key prefix.</returns>
    public string GetPrefix(int length = 8)
    {
        if (IsEmpty || Length <= length)
        {
            return Value;
        }

        return Value.Substring(0, length);
    }

    /// <summary>
    /// Checks if the API key starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to check for.</param>
    /// <returns>True if the API key starts with the prefix, false otherwise.</returns>
    public bool StartsWith(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        return Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the API key contains the specified substring.
    /// </summary>
    /// <param name="substring">The substring to check for.</param>
    /// <returns>True if the API key contains the substring, false otherwise.</returns>
    public bool Contains(string substring)
    {
        if (string.IsNullOrWhiteSpace(substring))
        {
            return false;
        }

        return Value.Contains(substring, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the string representation of the API key.
    /// </summary>
    /// <returns>The API key value.</returns>
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
        if (obj is not ApiKey other)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether two API keys are equal.
    /// </summary>
    /// <param name="left">The first API key to compare.</param>
    /// <param name="right">The second API key to compare.</param>
    /// <returns>True if the API keys are equal, false otherwise.</returns>
    public static bool operator ==(ApiKey? left, ApiKey? right)
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
    /// Determines whether two API keys are not equal.
    /// </summary>
    /// <param name="left">The first API key to compare.</param>
    /// <param name="right">The second API key to compare.</param>
    /// <returns>True if the API keys are not equal, false otherwise.</returns>
    public static bool operator !=(ApiKey? left, ApiKey? right)
    {
        return !(left == right);
    }
}
