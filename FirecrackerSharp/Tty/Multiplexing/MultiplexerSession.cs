using FirecrackerSharp.Tty.CompletionTracking;

namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class MultiplexerSession
{
    private readonly VmTtyClient _ttyClient;
    private readonly VmTtyMultiplexer _ttyMultiplexer;
    private long _lastCommandId = -1;
    internal readonly List<MultiplexerCommand> CommandsInternal = [];
    
    public long Id { get; }
    public ICapturePathGenerator CapturePathGenerator { get; set; } = ICapturePathGenerator.Default;
    public IReadOnlyList<MultiplexerCommand> Commands => CommandsInternal;

    internal MultiplexerSession(VmTtyClient ttyClient, VmTtyMultiplexer ttyMultiplexer, long id)
    {
        _ttyClient = ttyClient;
        _ttyMultiplexer = ttyMultiplexer;
        Id = id;
    }

    public MultiplexerCommand? GetCommandById(long id) => CommandsInternal.FirstOrDefault(c => c.Id == id);

    public MultiplexerCommand GetCommandByIdOrThrow(long id) => CommandsInternal.First(c => c.Id == id);

    public async Task<MultiplexerCommand> StartCommandAsync(
        string commandText,
        MultiplexedCaptureMode captureMode = MultiplexedCaptureMode.None,
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        completionTracker ??= new ExitSignalCompletionTracker();
        commandText = commandText.Trim();
        var commandId = Interlocked.Increment(ref _lastCommandId);

        string? capturePath = null;
        var sentCommand = $"screen -XS {Id} -p 0 stuff '{commandText}^M'";

        if (captureMode != MultiplexedCaptureMode.None)
        {
            capturePath = CapturePathGenerator.GetFilePath(Id, commandId, commandText, DateTimeOffset.UtcNow).Trim();

            var delimiter = captureMode == MultiplexedCaptureMode.StdoutAndStderr ? "&>" : ">";
            sentCommand = $"screen -XS {Id} -p 0 stuff '{commandText} {delimiter} {capturePath} ^M'";
        }

        await _ttyClient.BeginPrimaryWriteAsync(sentCommand, completionTracker: completionTracker,
            cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);

        var command = new MultiplexerCommand(_ttyClient, this, captureMode, capturePath, commandId);
        CommandsInternal.Add(command);
        return command;
    }

    public async Task QuitAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        completionTracker ??= new ExitSignalCompletionTracker();
        await _ttyClient.BeginPrimaryWriteAsync(
            $"screen -XS {Id} quit", completionTracker: completionTracker, cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);

        _ttyMultiplexer.SessionsInternal.Remove(this);
    }
}