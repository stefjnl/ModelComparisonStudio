using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.ValueObjects;

/// <summary>
/// Represents a prompt that will be sent to AI models.
/// </summary>
public class Prompt
{
    /// <summary>
    /// The prompt text content.
    /// </summary>
    [Required]
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// The length of the prompt in characters.
    /// </summary>
    public int Length => Content.Length;

    /// <summary>
    /// The estimated number of tokens in the prompt.
    /// </summary>
    public int EstimatedTokenCount => EstimateTokenCount(Content);

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private Prompt() { }

    /// <summary>
    /// Creates a new prompt with the specified content.
    /// </summary>
    /// <param name="content">The prompt content.</param>
    /// <returns>A new prompt instance.</returns>
    public static Prompt Create(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Prompt content cannot be null or empty.", nameof(content));
        }

        if (content.Length > 50000)
        {
            throw new ArgumentOutOfRangeException(nameof(content), "Prompt content cannot exceed 50,000 characters.");
        }

        return new Prompt
        {
            Content = content.Trim()
        };
    }

    /// <summary>
    /// Creates a prompt from a string without validation (for internal use).
    /// </summary>
    /// <param name="content">The prompt content.</param>
    /// <returns>A new prompt instance.</returns>
    internal static Prompt FromString(string content)
    {
        return new Prompt
        {
            Content = content
        };
    }

    /// <summary>
    /// Creates an empty prompt.
    /// </summary>
    /// <returns>A new empty prompt instance.</returns>
    public static Prompt CreateEmpty()
    {
        return new Prompt
        {
            Content = string.Empty
        };
    }

    /// <summary>
    /// Checks if the prompt is empty.
    /// </summary>
    /// <returns>True if the prompt is empty, false otherwise.</returns>
    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(Content);
    }

    /// <summary>
    /// Checks if the prompt is within the specified length limit.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <returns>True if the prompt is within the limit, false otherwise.</returns>
    public bool IsWithinLengthLimit(int maxLength)
    {
        return Length <= maxLength;
    }

    /// <summary>
    /// Gets a truncated version of the prompt for display purposes.
    /// </summary>
    /// <param name="maxLength">The maximum length for the truncated version.</param>
    /// <returns>A truncated version of the prompt.</returns>
    public string GetTruncated(int maxLength)
    {
        if (Length <= maxLength)
        {
            return Content;
        }

        return Content.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Gets a preview of the prompt (first few lines).
    /// </summary>
    /// <param name="maxLines">The maximum number of lines to include.</param>
    /// <returns>A preview of the prompt.</returns>
    public string GetPreview(int maxLines = 3)
    {
        var lines = Content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var previewLines = lines.Take(maxLines).ToList();

        if (lines.Length > maxLines)
        {
            previewLines.Add("...");
        }

        return string.Join(Environment.NewLine, previewLines);
    }

    /// <summary>
    /// Checks if the prompt contains specific keywords.
    /// </summary>
    /// <param name="keywords">The keywords to search for.</param>
    /// <returns>True if any keyword is found, false otherwise.</returns>
    public bool ContainsKeywords(params string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
        {
            return false;
        }

        return keywords.Any(keyword =>
            Content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Estimates the number of tokens in the prompt.
    /// This is a rough estimation based on character count.
    /// </summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <returns>An estimated token count.</returns>
    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Rough estimation: 1 token ≈ 4 characters for English text
        // This is a simplification and actual tokenization may vary
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    /// <summary>
    /// Returns the string representation of the prompt.
    /// </summary>
    /// <returns>The prompt content.</returns>
    public override string ToString()
    {
        return Content;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>True if the specified object is equal to the current object, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Prompt other)
        {
            return false;
        }

        return Content.Equals(other.Content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return Content.GetHashCode(StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether two prompts are equal.
    /// </summary>
    /// <param name="left">The first prompt to compare.</param>
    /// <param name="right">The second prompt to compare.</param>
    /// <returns>True if the prompts are equal, false otherwise.</returns>
    public static bool operator ==(Prompt? left, Prompt? right)
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
    /// Determines whether two prompts are not equal.
    /// </summary>
    /// <param name="left">The first prompt to compare.</param>
    /// <param name="right">The second prompt to compare.</param>
    /// <returns>True if the prompts are not equal, false otherwise.</returns>
    public static bool operator !=(Prompt? left, Prompt? right)
    {
        return !(left == right);
    }
}
