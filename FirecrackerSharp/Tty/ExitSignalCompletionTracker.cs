namespace FirecrackerSharp.Tty;

public class ExitSignalCompletionTracker : ICompletionTracker
{
    public VmTtyClient TtyClient { get; set; } = null!;

    private string? _currentExitSignal;
    private string? _currentCommand;
    private readonly Func<string> _exitSignalGenerator;
    
    public ExitSignalCompletionTracker(Func<string> exitSignalGenerator)
    {
        _exitSignalGenerator = exitSignalGenerator;
    }
    
    public ExitSignalCompletionTracker(int lowerBound = 1, int upperBound = 100000, string prefix = "")
    {
        _exitSignalGenerator = () =>
        {
            var number = Random.Shared.Next(lowerBound, upperBound);
            return prefix + number;
        };
    }
    
    public string TransformInput(string inputText)
    {
        _currentExitSignal = _exitSignalGenerator();
        inputText = inputText.Trim();
        _currentCommand = inputText + ";echo " + _currentExitSignal;
        return _currentCommand;
    }

    public bool ShouldCapture(string line)
    {
        line = line.Trim();
        return line != _currentExitSignal && !line.Contains(_currentCommand!);
    }

    public bool CheckReactively(string line)
    {
        return line.Trim() == _currentExitSignal;
    }

    // passive checking is not supported for this action tracker
    public Task<bool>? CheckPassively() => null;
}