namespace FirecrackerSharp.Tty.CompletionTracking;

public class StringMatchCompletionTracker(
    StringMatchMode stringMatchMode,
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

    public bool CheckReactively(string line)
    {
        if (line.Trim() == Context!.InputText) return false;
        
        return stringMatchMode switch
        {
            StringMatchMode.Contains => line.Contains(value, stringComparison),
            StringMatchMode.StartsWith => line.StartsWith(value, stringComparison),
            StringMatchMode.EndsWith => line.EndsWith(value, stringComparison),
            _ => throw new ArgumentOutOfRangeException(nameof(stringMatchMode), stringMatchMode, null)
        };
    }

    public Task<bool>? CheckPassively() => null;
}