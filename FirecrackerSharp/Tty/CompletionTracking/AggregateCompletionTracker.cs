namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// An <see cref="ICompletionTracker"/> that combines/aggregates multiple underlying <see cref="ICompletionTracker"/>s
/// by using <see cref="NegotiationPolicy"/>s to negotiate between their decisions into a final verdict.
/// </summary>
/// <param name="aggregatedTrackers">The <see cref="IEnumerable{ICompletionTracker}"/> that represents the trackers
/// that should be aggregated</param>
/// <param name="captureNegotiationPolicy">The <see cref="NegotiationPolicy"/> used when deciding whether to capture
/// a line to the output buffer</param>
/// <param name="checkNegotiationPolicy">The <see cref="NegotiationPolicy"/> used when deciding when checking for
/// completion</param>
public sealed class AggregateCompletionTracker(
    IEnumerable<ICompletionTracker> aggregatedTrackers,
    NegotiationPolicy captureNegotiationPolicy = NegotiationPolicy.All,
    NegotiationPolicy checkNegotiationPolicy = NegotiationPolicy.All) : ICompletionTracker
{
    private CompletionTrackerContext? _context;

    public CompletionTrackerContext? Context
    {
        get => _context;
        set
        {
            _context = value;
            foreach (var tracker in aggregatedTrackers)
            {
                tracker.Context = value;
            }
        }
    }
    
    public string TransformInput(string inputText)
    {
        return aggregatedTrackers
            .Aggregate(inputText, (currentText, tracker) => tracker.TransformInput(currentText));
    }

    public bool ShouldCapture(string line)
    {
        return ApplyNegotiationPolicy(
            aggregatedTrackers
                .Select(t => t.ShouldCapture(line))
                .ToList(),
            captureNegotiationPolicy);
    }

    public bool Check(string line)
    {
        return ApplyNegotiationPolicy(
            aggregatedTrackers
                .Select(t => t.Check(line))
                .ToList(),
            checkNegotiationPolicy);
    }

    private static bool ApplyNegotiationPolicy(List<bool> inputs, NegotiationPolicy negotiationPolicy)
    {
        return negotiationPolicy switch
        {
            NegotiationPolicy.Any => inputs.Any(x => x),
            NegotiationPolicy.Majority => inputs.Count(x => x) > inputs.Count(x => !x),
            NegotiationPolicy.All => inputs.All(x => x),
            _ => throw new ArgumentOutOfRangeException(nameof(negotiationPolicy), negotiationPolicy, null)
        };
    }
}