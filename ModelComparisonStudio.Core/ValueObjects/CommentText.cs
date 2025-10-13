using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.ValueObjects;

/// <summary>
/// Represents a comment text with validation and maximum length constraints.
/// </summary>
public class CommentText
{
    /// <summary>
    /// Maximum allowed length for comments.
    /// </summary>
    public const int MaxLength = 500;

    /// <summary>
    /// The comment text value.
    /// </summary>
    [Required]
    [MaxLength(MaxLength)]
    public string Value { get; }

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private CommentText()
    {
        Value = null!;
    }

    /// <summary>
    /// Private constructor with value.
    /// </summary>
    /// <param name="value">The comment text value.</param>
    private CommentText(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new CommentText with the specified value.
    /// </summary>
    /// <param name="value">The comment text.</param>
    /// <returns>A new CommentText instance.</returns>
    public static CommentText Create(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"Comment cannot exceed {MaxLength} characters.", nameof(value));

        return new CommentText(value);
    }

    /// <summary>
    /// Creates an empty CommentText.
    /// </summary>
    /// <returns>An empty CommentText instance.</returns>
    public static CommentText CreateEmpty()
    {
        return new CommentText(string.Empty);
    }

    /// <summary>
    /// Checks if the comment is empty.
    /// </summary>
    /// <returns>True if the comment is empty, false otherwise.</returns>
    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(Value);
    }

    /// <summary>
    /// Returns the string representation of the CommentText.
    /// </summary>
    /// <returns>The string value.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return obj is CommentText other && Value == other.Value;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">The left CommentText.</param>
    /// <param name="right">The right CommentText.</param>
    /// <returns>True if equal, false otherwise.</returns>
    public static bool operator ==(CommentText? left, CommentText? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">The left CommentText.</param>
    /// <param name="right">The right CommentText.</param>
    /// <returns>True if not equal, false otherwise.</returns>
    public static bool operator !=(CommentText? left, CommentText? right)
    {
        return !Equals(left, right);
    }
}