using FirecrackerSharp.Tty.CompletionTracking;

namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class VmTtyMultiplexer
{
    private readonly VmTtyClient _ttyClient;
    internal readonly List<MultiplexerSession> SessionsInternal = [];
    private long _lastSessionId = -1;

    public IReadOnlyList<MultiplexerSession> Sessions => SessionsInternal;

    internal VmTtyMultiplexer(VmTtyClient ttyClient)
    {
        _ttyClient = ttyClient;
    }

    public async Task<MultiplexerSession> StartSessionAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        completionTracker ??= new ExitSignalCompletionTracker();
        var sessionId = Interlocked.Increment(ref _lastSessionId);

        await _ttyClient.BeginPrimaryWriteAsync(
            $"screen -dmS {sessionId}", completionTracker: completionTracker, cancellationToken: cancellationToken);
        await _ttyClient.WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken: cancellationToken);
        
        var session = new MultiplexerSession(_ttyClient, this, sessionId);
        SessionsInternal.Add(session);
        return session;
    }

    public MultiplexerSession? GetSessionById(long id) => SessionsInternal.FirstOrDefault(s => s.Id == id);

    public MultiplexerSession GetSessionByIdOrThrow(long id) => SessionsInternal.First(s => s.Id == id);

    public async Task QuitAllSessionsAsync(
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = SessionsInternal
            .Select(s => s.QuitAsync(completionTracker, pollTimeSpan, cancellationToken));
        await Task.WhenAll(tasks).WaitAsync(cancellationToken);
    }
}