namespace ModelComparisonStudio.Models;

/// <summary>
/// Defines the execution mode for model comparisons
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Execute models in parallel for better performance
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute models sequentially for compatibility or debugging
    /// </summary>
    Sequential
}