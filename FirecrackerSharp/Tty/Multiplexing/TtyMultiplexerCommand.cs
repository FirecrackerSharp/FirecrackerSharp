using FirecrackerSharp.Tty.CompletionTracking;
using FirecrackerSharp.Tty.OutputBuffering;

namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class TtyMultiplexerCommand
{
    public ulong Id { get; }
    public CaptureMode CaptureMode { get; }
    public string CommandText { get; }

    private readonly string? _capturePath;
    private readonly VmTtyClient _ttyClient;

    internal TtyMultiplexerCommand(ulong id, CaptureMode captureMode, string commandText, string? capturePath,
        VmTtyClient ttyClient)
    {
        Id = id;
        CaptureMode = captureMode;
        CommandText = commandText;
        _capturePath = capturePath;
        _ttyClient = ttyClient;
    }

    public async Task SendStdinAsync(
        string stdinText,
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        completionTracker ??= new ExitSignalCompletionTracker();
        await _ttyClient.BeginPrimaryWriteAsync($"screen -p 0 -XS {Id} stuff \"{stdinText} ^M\"",
            completionTracker: completionTracker, cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
    }

    public async Task SendCtrlCAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        completionTracker ??= new ExitSignalCompletionTracker();
        await _ttyClient.BeginPrimaryWriteAsync($"screen -p 0 -XS {Id} stuff \"^C\"",
            completionTracker: completionTracker, cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
    }

    public async Task EndSessionAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        completionTracker ??= new ExitSignalCompletionTracker();
        await _ttyClient.BeginPrimaryWriteAsync($"screen -XS {Id} quit", completionTracker: completionTracker,
            cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
    }

    public async Task CaptureOutputAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        if (_capturePath is null)
        {
            throw new CaptureWasNotInitializedException(
                "Capturing was not initialized for this command when it was being created");
        }

        completionTracker ??= new ExitSignalCompletionTracker();
        await _ttyClient.BeginPrimaryWriteAsync($"cat {_capturePath}", completionTracker: completionTracker,
            cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
    }

    public async Task<string?> CaptureOutputIntoMemoryAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        if (_ttyClient.OutputBuffer is not MemoryOutputBuffer)
        {
            _ttyClient.OutputBuffer = new MemoryOutputBuffer();
        }

        var memoryBuffer = (MemoryOutputBuffer)_ttyClient.OutputBuffer;
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
        _ttyClient.OutputBuffer = memoryBuffer;
        await CaptureOutputAsync(completionTracker, pollTimeSpan, cancellationToken);
        return memoryBuffer.LastCommit;
    }
}