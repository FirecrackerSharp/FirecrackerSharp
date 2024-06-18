namespace FirecrackerSharp.Tty;

public interface ICompletionTracker
{
    VmTtyClient TtyClient { get; set; }
    
    public string TransformInput(string inputText);

    public bool ShouldCapture(string line);

    public bool CheckReactively(string line);

    public Task<bool>? CheckPassively();
}