namespace FirecrackerSharp.Tty.CompletionTracking;

public interface ICompletionTracker
{
    CompletionTrackerContext? Context { get; set; }
    
    public string TransformInput(string inputText);

    public bool ShouldCapture(string line);

    public bool CheckReactively(string line);

    public Task<bool>? CheckPassively();
}