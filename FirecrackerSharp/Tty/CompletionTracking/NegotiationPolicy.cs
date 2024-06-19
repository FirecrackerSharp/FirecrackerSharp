namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// A negotiation policy defines a condition that needs to be fulfilled in order for a set of votes/booleans
/// (positive or negative) to amount to a positive vote/true when being aggregated.
/// </summary>
public enum NegotiationPolicy
{
    /// <summary>
    /// Any of the votes must be positive for the outcome to be positive
    /// </summary>
    Any,
    /// <summary>
    /// The amount of positive votes should outweigh the amount of negative votes for the outcome to be positive
    /// </summary>
    Majority,
    /// <summary>
    /// All the votes must be positive for the outcome to be positive
    /// </summary>
    All
}