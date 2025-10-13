using System.ComponentModel.DataAnnotations;

namespace ModelComparisonStudio.Core.ValueObjects;

/// <summary>
/// Represents a unique identifier for an evaluation.
/// </summary>
public class EvaluationId
{
    /// <summary>
    /// The unique identifier value.
    /// </summary>
    [Required]
    public string Value { get; }

    /// <summary>
    /// Private constructor for Entity Framework or other ORMs.
    /// </summary>
    private EvaluationId()
    {
        Value = null!;
    }

    /// <summary>
    /// Private constructor with value.
    /// </summary>
    /// <param name="value">The unique identifier value.</param>
    private EvaluationId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new EvaluationId with a generated GUID.
    /// </summary>
    /// <returns>A new EvaluationId instance.</returns>
    public static EvaluationId Create()
    {
        return new EvaluationId(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Creates an EvaluationId from an existing string value.
    /// </summary>
    /// <param name="value">The string value to use as the ID.</param>
    /// <returns>A new EvaluationId instance.</returns>
    public static EvaluationId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Evaluation ID cannot be null or empty.", nameof(value));

        return new EvaluationId(value);
    }

    /// <summary>
    /// Returns the string representation of the EvaluationId.
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
        return obj is EvaluationId other && Value == other.Value;
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
    /// <param name="left">The left EvaluationId.</param>
    /// <param name="right">The right EvaluationId.</param>
    /// <returns>True if equal, false otherwise.</returns>
    public static bool operator ==(EvaluationId? left, EvaluationId? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">The left EvaluationId.</param>
    /// <param name="right">The right EvaluationId.</param>
    /// <returns>True if not equal, false otherwise.</returns>
    public static bool operator !=(EvaluationId? left, EvaluationId? right)
    {
        return !Equals(left, right);
    }
}