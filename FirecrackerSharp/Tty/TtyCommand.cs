namespace FirecrackerSharp.Tty;

public class TtyCommand
{
    private readonly VmTty _tty;

    public TtyCommandOptions Options { get; }
    public string CurrentOutput { get; private set; } = string.Empty;
    
    internal TtyCommand(VmTty tty, TtyCommandOptions options)
    {
        _tty = tty;
        Options = options;
    }

    public async Task<bool> AwaitAndReadAsync(uint timeoutSeconds = 3, uint pollMillis = 10)
    {
        var source = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var skipFirstLine = true;

        while (!source.IsCancellationRequested)
        {
            var appendix = await _tty.ReadNewAsync(new CancellationTokenSource(Options.ReadTimeoutTimeSpan), skipFirstLine);

            if (skipFirstLine) skipFirstLine = false;
            
            if (appendix is null) return true;

            CurrentOutput += appendix;

            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(TimeSpan.FromMilliseconds(pollMillis));
        }

        return false;
    }

    public async Task StopAsync(uint timeoutSeconds = 1)
    {
        var source = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await _tty.WriteNewAsync(source, Options.ExitSignal, Options.NewlineAfterExitSignal);
        await ReadAsync();
    }

    public async Task ReadAsync()
    {
        CurrentOutput +=
            await _tty.ReadNewAsync(new CancellationTokenSource(Options.ReadTimeoutTimeSpan), skipFirstLine: false);
    }

    public async Task WriteToInputAsync(string content, uint timeoutSeconds = 1)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        await _tty.WriteNewAsync(source, content);
    }
}