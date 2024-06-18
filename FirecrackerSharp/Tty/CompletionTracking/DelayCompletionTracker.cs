namespace FirecrackerSharp.Tty.CompletionTracking;

public class DelayCompletionTracker(
    TimeSpan delayTimeSpan,
    bool excludeContainingCommand = true) : ICompletionTracker
{
    public CompletionTrackerContext? Context { get; set; }

    public string TransformInput(string inputText) => inputText;

    public bool ShouldCapture(string line)
    {
        if (!excludeContainingCommand) return true;
        return !line.Contains(Context!.InputText);
    }

    public bool CheckReactively(string line) => false;

    public async Task<bool> CheckPassively()
    {
        await Task.Delay(delayTimeSpan);
        return true;
    }
}