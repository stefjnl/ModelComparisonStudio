namespace ModelComparisonStudio.Core.Entities;

/// <summary>
/// Represents the status of a model result execution.
/// </summary>
public enum ModelResultStatus
{
    /// <summary>
    /// The model execution was successful.
    /// </summary>
    Success,

    /// <summary>
    /// The model execution resulted in an error.
    /// </summary>
    Error,

    /// <summary>
    /// The model execution timed out.
    /// </summary>
    Timeout
}
