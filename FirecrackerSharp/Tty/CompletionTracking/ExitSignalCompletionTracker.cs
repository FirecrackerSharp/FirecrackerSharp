namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// The <see cref="ICompletionTracker"/> that is recommended for most scenarios. It uses a rudimentary mechanism:
/// chooses an exit signal (=x), appends ";echo x" to the input text so that regardless if the command was successful
/// or not the exit signal will be printed out after the command's completion, and then scans reactively for the
/// appearance of that exit signal.
///
/// Preferably, the exit signal should be unique for every write operation and that is facilitated by a generator
/// function passed into this tracker.
/// </summary>
public sealed class ExitSignalCompletionTracker : ICompletionTracker
{
    public CompletionTrackerContext? Context { get; set; }

    private string? _currentExitSignal;
    private readonly Func<string> _exitSignalGenerator;
    
    /// <summary>
    /// Create an <see cref="ExitSignalCompletionTracker"/> with a custom function that produces the exit signal.
    /// </summary>
    /// <param name="exitSignalGenerator">The producer of the exit signal</param>
    public ExitSignalCompletionTracker(Func<string> exitSignalGenerator)
    {
        _exitSignalGenerator = exitSignalGenerator;
    }
    
    /// <summary>
    /// Create an <see cref="ExitSignalCompletionTracker"/> with an exit signal producer that appends a random integer
    /// to a given prefix
    /// </summary>
    /// <param name="lowerBound">The lowest possible appended integer</param>
    /// <param name="upperBound">The highest possible appended integer</param>
    /// <param name="prefix">The prefix before the integer</param>
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
        return inputText + ";echo " + _currentExitSignal;
    }

    public bool ShouldCapture(string line)
    {
        line = line.Trim();
        return line != _currentExitSignal && !line.Contains(Context!.InputText);
    }

    public bool CheckReactively(string line)
    {
        return line.Trim() == _currentExitSignal;
    }

    // passive checking is not supported for this action tracker
    public Task<bool>? CheckPassively() => null;
}