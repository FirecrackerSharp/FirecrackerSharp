using FirecrackerSharp.Tty.CompletionTracking;

namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class VmTtyMultiplexer
{
    private readonly VmTtyClient _ttyClient;
    private ulong _lastCommandId;

    internal VmTtyMultiplexer(VmTtyClient ttyClient)
    {
        _ttyClient = ttyClient;
    }

    public async Task<TtyMultiplexerCommand> StartCommandAsync(
        string commandText,
        CaptureMode captureMode = CaptureMode.None,
        ICapturePathGenerator? capturePathGenerator = null,
        ICompletionTracker? initCompletionTracker = null,
        ICompletionTracker? sendCompletionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        // init defaults
        capturePathGenerator ??= ICapturePathGenerator.Default;
        initCompletionTracker ??= new ExitSignalCompletionTracker();
        sendCompletionTracker ??= new ExitSignalCompletionTracker();
        
        // set up objects
        var commandId = Interlocked.Increment(ref _lastCommandId);
        var capturePath = captureMode == CaptureMode.None
            ? null
            : capturePathGenerator.GetCapturePath(commandId, commandText, captureMode);
        var multiplexerCommand = new TtyMultiplexerCommand(commandId, captureMode, commandText, capturePath);
        
        // issue initializing write
        await _ttyClient.BeginPrimaryWriteAsync($"screen -dmS {commandId}", completionTracker: initCompletionTracker,
            cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
        
        // find exact command text, taking capture mode into account
        var issuingCommandText = $"screen -X -p 0 -S {commandId} stuff \"{commandText} ^M\"";
        if (captureMode != CaptureMode.None)
        {
            var delimiter = captureMode == CaptureMode.StandardOutput ? ">" : "&>";
            issuingCommandText = $"screen -X -p 0 -S {commandId} stuff \"{commandText} {delimiter} {capturePath} ^M\"";
        }

        // issue command-sending write
        await _ttyClient.BeginPrimaryWriteAsync(issuingCommandText, completionTracker: sendCompletionTracker,
            cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);

        return multiplexerCommand;
    }
}