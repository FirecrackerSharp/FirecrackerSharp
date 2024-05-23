namespace FirecrackerSharp.Tty;

public class TtyCommand
{
    private readonly VmTty _tty;
    
    public string Command { get; private init; }
    public string Arguments { get; private init; }
    public TimeSpan ReadTimeoutTimeSpan { get; }
    public string CurrentOutput { get; private set; } = string.Empty;
    
    internal TtyCommand(VmTty tty, string command, string arguments, TimeSpan readTimeoutTimeSpan)
    {
        _tty = tty;
        
        Command = command;
        Arguments = arguments;
        ReadTimeoutTimeSpan = readTimeoutTimeSpan;
    }

    public async Task<bool> AwaitAndReadAsync(uint timeoutSeconds = 3, uint pollMillis = 10)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        while (!source.IsCancellationRequested)
        {
            var appendix = await _tty.ReadNewAsync(new CancellationTokenSource(ReadTimeoutTimeSpan));
            if (appendix is null) return true;

            CurrentOutput += appendix;

            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(TimeSpan.FromMilliseconds(pollMillis));
        }

        return false;
    }

    public async Task WriteToInputAsync(string content, uint timeoutSeconds = 1)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        await _tty.WriteNewAsync(source, content);
    }
}