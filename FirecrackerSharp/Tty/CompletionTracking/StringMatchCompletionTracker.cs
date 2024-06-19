namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// A <see cref="ICompletionTracker"/> that reactively matches the streamed-in lines against a given string value via
/// a certain <see cref="StringMatchOperation"/> and completes when the match is found.
/// </summary>
/// <param name="stringMatchOperation">The <see cref="StringMatchOperation"/> used for determining which operation to apply
/// to the two strings</param>
/// <param name="value">The string value to match against</param>
/// <param name="stringComparison">The type of <see cref="StringComparison"/>, case-sensitive by default</param>
/// <param name="excludeContainingCommand">Whether to exclude the line containing the original command from the
/// output buffer, true by default</param>
public class StringMatchCompletionTracker(
    StringMatchOperation stringMatchOperation,
    string value,
    StringComparison stringComparison = StringComparison.Ordinal,
    bool excludeContainingCommand = true) : ICompletionTracker
{
    public CompletionTrackerContext? Context { get; set; }

    public string TransformInput(string inputText) => inputText;

    public bool ShouldCapture(string line)
    {
        if (!excludeContainingCommand) return true;
        return !line.Contains(Context!.InputText);
    }

    public bool Check(string line)
    {
        line = line.Trim('\n');
        
        return stringMatchOperation switch
        {
            StringMatchOperation.Contains => line.Contains(value, stringComparison),
            StringMatchOperation.StartsWith => line.StartsWith(value, stringComparison),
            StringMatchOperation.EndsWith => line.EndsWith(value, stringComparison),
            StringMatchOperation.Equals => line.Equals(value, stringComparison),
            _ => throw new ArgumentOutOfRangeException(nameof(stringMatchOperation), stringMatchOperation, null)
        };
    }
}