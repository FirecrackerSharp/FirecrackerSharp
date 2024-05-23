namespace FirecrackerSharp.Terminal;

public class TerminalCommand
{
    private readonly VmTerminal _terminal;
    
    public string Command { get; private init; }
    public string Arguments { get; private init; }
    public string CurrentOutput { get; private set; } = string.Empty;
    
    internal TerminalCommand(VmTerminal terminal, string command, string arguments)
    {
        _terminal = terminal;
        Command = command;
        Arguments = arguments;
    }

    public async Task<bool> WaitUntilCompletionAsync(uint timeoutSeconds = 3, uint pollMillis = 10)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        while (true)
        {
            if (source.IsCancellationRequested) return false;

            var appendix = _terminal.ReadNew(source);
            if (appendix.TrimEnd().EndsWith('$')) return true;
            
            CurrentOutput += appendix;
            
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(TimeSpan.FromMilliseconds(pollMillis));
        }
    }

    public async Task WriteToInputAsync(string content, uint timeoutSeconds = 1)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        await _terminal.WriteNewAsync(source, content);
    }
}