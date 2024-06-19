namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// An operation to use for matching a line and the constant value in a <see cref="StringMatchCompletionTracker"/>
/// </summary>
public enum StringMatchOperation
{
    /// <summary>
    /// The line should contain the value
    /// </summary>
    Contains,
    /// <summary>
    /// The line should start with the value
    /// </summary>
    StartsWith,
    /// <summary>
    /// The line should end with the value
    /// </summary>
    EndsWith,
    /// <summary>
    /// The line should be equal to the value
    /// </summary>
    Equals
}