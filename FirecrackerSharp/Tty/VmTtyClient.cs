using System.Text;
using FirecrackerSharp.Core;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tty.CompletionTracking;
using FirecrackerSharp.Tty.OutputBuffering;

namespace FirecrackerSharp.Tty;

/// <summary>
/// An advanced interface used to interact with a microVM's TTY.
///
/// It utilizes <see cref="IOutputBuffer"/>s for stdout & stderr capture, <see cref="ICompletionTracker"/>s for state
/// management, and separates write operations into primary and intermittent ones (intermittent allowing for writes
/// needed for the primary command).
///
/// The high-level APIs of <see cref="RunBufferedCommandAsync"/>, <see cref="StartBufferedCommandAsync"/>,
/// <see cref="WaitForBufferedCommandAsync"/> and <see cref="TryGetMemoryBufferState"/> cover the basic usage case,
/// while the lower-level APIs are intended for scenarios where leveraging the full capabilities of output buffering
/// and completion tracking is viable.
/// </summary>
public sealed class VmTtyClient
{
    private readonly Vm _vm;
    private readonly SemaphoreSlim _primaryWriteSemaphore = new(1, 1);
    private readonly SemaphoreSlim _intermittentWriteSemaphore = new(1, 1);
    
    private ICompletionTracker? _primaryCompletionTracker;
    private ICompletionTracker? _intermittentCompletionTracker;
    private IOutputBuffer? _currentOutputBuffer;
    
    /// <summary>
    /// Whether the client is currently accepting a primary write. If false, another primary write is currently taking
    /// place and hasn't been completed manually or tracked. Use <see cref="WaitForPrimaryAvailabilityAsync"/> to
    /// await.
    /// </summary>
    public bool IsAvailableForPrimaryWrite => _primaryWriteSemaphore.CurrentCount > 0;
    /// <summary>
    /// Whether the client is currently accepting an intermittent write. If false, another intermittent write is
    /// currently taking place and hasn't been completed manually or tracked.
    /// </summary>
    public bool IsAvailableForIntermittentWrite => _intermittentWriteSemaphore.CurrentCount > 0;
    
    /// <summary>
    /// The currently active <see cref="IOutputBuffer"/>, or null if none has been configured. Assigning this an
    /// <see cref="IOutputBuffer"/> will open it.
    /// </summary>
    public IOutputBuffer? OutputBuffer
    {
        get => _currentOutputBuffer;
        set
        {
            _currentOutputBuffer = value;
            _currentOutputBuffer?.Open();
        }
    }
    
    internal VmTtyClient(Vm vm)
    {
        _vm = vm;
    }

    internal void StartListening()
    {
        _vm.Process!.OutputReceived += (_, line) =>
        {
            switch (_vm.Lifecycle.CurrentPhase)
            {
                case VmLifecyclePhase.Booting:
                    _vm.Lifecycle.BootLogTarget.Receive(line);
                    break;
                case VmLifecyclePhase.Active:
                    _vm.Lifecycle.ActiveLogTarget.Receive(line);
                    break;
                case VmLifecyclePhase.ShuttingDown:
                    _vm.Lifecycle.ShutdownLogTarget.Receive(line);
                    break;
                case VmLifecyclePhase.NotBooted:
                    return;
                case VmLifecyclePhase.PoweredOff:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_vm.Lifecycle.IsNotActive) return;
            
            var shouldCapture = true;
            var shouldCompletePrimary = false;
            var shouldCompleteIntermittent = false;
            
            if (_primaryCompletionTracker is not null)
            {
                shouldCompletePrimary = _primaryCompletionTracker.Check(line);
                shouldCapture = _primaryCompletionTracker.ShouldCapture(line);
            }

            if (_intermittentCompletionTracker is not null)
            {
                shouldCompleteIntermittent = _intermittentCompletionTracker.Check(line);
            }
            
            if (shouldCapture && OutputBuffer is not null)
            {
                OutputBuffer.Receive(line);
            }

            if (shouldCompletePrimary) CompletePrimaryWrite();
            if (shouldCompleteIntermittent) CompleteIntermittentWrite();
        };
    }

    /// <summary>
    /// Await the completion of the currently active primary write.
    /// </summary>
    /// <param name="pollTimeSpan">The frequency at which the underlying semaphore should be polled for changes</param>
    /// <param name="forceCompletionAfterTimeout">Whether the completion of the primary write should be forced
    /// in case of a timeout</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor while awaiting</param>
    /// <returns>Whether the wait was successful or not</returns>
    public Task<bool> WaitForPrimaryAvailabilityAsync(
        TimeSpan? pollTimeSpan = null,
        bool forceCompletionAfterTimeout = false,
        CancellationToken cancellationToken = default)
    {
        Action? completer = forceCompletionAfterTimeout ? CompletePrimaryWrite : null;
        return PollForSemaphoreAsync(_primaryWriteSemaphore, pollTimeSpan, completer, cancellationToken);
    }

    /// <summary>
    /// Await the completion of the currently active intermittent write.
    /// </summary>
    /// <param name="pollTimeSpan">The frequency at which the underlying semaphore should be polled for changes</param>
    /// <param name="forceCompletionAfterTimeout">Whether the completion of the intermittent write should be forced
    /// in case of a timeout</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor while awaiting</param>
    /// <returns>Whether the wait was successful or not</returns>
    public Task<bool> WaitForIntermittentAvailabilityAsync(
        TimeSpan? pollTimeSpan = null,
        bool forceCompletionAfterTimeout = false,
        CancellationToken cancellationToken = default)
    {
        Action? completer = forceCompletionAfterTimeout ? CompleteIntermittentWrite : null;
        return PollForSemaphoreAsync(_intermittentWriteSemaphore, pollTimeSpan, completer, cancellationToken);
    }

    /// <summary>
    /// Begin a new primary write operation.
    /// </summary>
    /// <param name="inputText">The input text to be inputted into the TTY</param>
    /// <param name="insertNewline">Whether a newline should be inserted following the text</param>
    /// <param name="completionTracker">A <see cref="ICompletionTracker"/> to track the completion of this primary
    /// write, if null then no tracking will be performed and the user will need to manually control the completion
    /// by invoking <see cref="CompletePrimaryWrite"/></param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor</param>
    public async Task BeginPrimaryWriteAsync(
        string inputText,
        bool insertNewline = true,
        ICompletionTracker? completionTracker = null,
        CancellationToken cancellationToken = default)
    {
        await _primaryWriteSemaphore.WaitAsync(cancellationToken);

        if (completionTracker is not null)
        {
            inputText = completionTracker.TransformInput(inputText);
        }

        await WriteInternalAsync(inputText, insertNewline,
            () =>
            {
                if (completionTracker is null) return;

                completionTracker.Context = new CompletionTrackerContext(
                    TtyClient: this,
                    TrackingStartTime: DateTimeOffset.UtcNow,
                    InputText: inputText);
                _primaryCompletionTracker = completionTracker;
            },
            cancellationToken);
    }

    /// <summary>
    /// Begin a new intermittent write operation.
    /// </summary>
    /// <param name="inputText">The input text to be inputted into the TTY</param>
    /// <param name="insertNewline">Whether a newline should be inserted following the text</param>
    /// <param name="completionTracker">A <see cref="ICompletionTracker"/> to track the completion of this intermittent
    /// write, if null then no tracking will be performed and the user will need to manually control the completion
    /// by invoking <see cref="CompleteIntermittentWrite"/></param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor</param>
    public async Task BeginIntermittentWriteAsync(
        string inputText,
        bool insertNewline = true,
        ICompletionTracker? completionTracker = null,
        CancellationToken cancellationToken = default)
    {
        await _intermittentWriteSemaphore.WaitAsync(cancellationToken);

        if (completionTracker is not null)
        {
            inputText = completionTracker.TransformInput(inputText);
        }

        await WriteInternalAsync(inputText, insertNewline,
            () =>
            {
                if (completionTracker is null) return;

                completionTracker.Context = new CompletionTrackerContext(
                    TtyClient: this,
                    TrackingStartTime: DateTimeOffset.UtcNow,
                    InputText: inputText);
                _intermittentCompletionTracker = completionTracker;
            },
            cancellationToken);
    }

    /// <summary>
    /// Manually complete the ongoing primary write. This is primarily necessary when the primary write is untracked
    /// and the user takes on responsibility to track it themselves and call this manually.
    /// </summary>
    public void CompletePrimaryWrite()
    {
        if (!IsAvailableForPrimaryWrite) _primaryWriteSemaphore.Release();
        _primaryCompletionTracker = null;
        OutputBuffer?.Commit();
    }

    /// <summary>
    /// Manually complete the ongoing intermittent write. This is primarily necessary when the intermittent write is untracked
    /// and the user takes on responsibility to track it themselves and call this manually.
    /// </summary>
    public void CompleteIntermittentWrite()
    {
        if (!IsAvailableForIntermittentWrite) _intermittentWriteSemaphore.Release();
        _intermittentCompletionTracker = null;
    }

    /// <summary>
    /// Run a given command, await its completion and return the contents from an in-memory buffer.
    /// Internally, this sets up a <see cref="MemoryOutputBuffer"/>, a <see cref="ExitSignalCompletionTracker"/> (by
    /// default), triggers a primary write, awaits its completion and returns the last commit in the buffer.
    /// </summary>
    /// <param name="commandText">The command text to be inserted</param>
    /// <param name="insertNewline">Whether a newline should be put after the command text</param>
    /// <param name="completionTracker">The custom <see cref="ICompletionTracker"/> to use for monitoring this command
    /// (untracked writes are unsupported), <see cref="ExitSignalCompletionTracker"/> is used by default</param>
    /// <param name="pollTimeSpan">The frequency at which the semaphore should be polled</param>
    /// <param name="forceCompletionAfterTimeout">Whether to force completion after the wait has timed out</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The buffer content, or null if the buffer is no longer a <see cref="MemoryOutputBuffer"/></returns>
    public async Task<string?> RunBufferedCommandAsync(
        string commandText,
        bool insertNewline = true,
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        bool forceCompletionAfterTimeout = false,
        CancellationToken cancellationToken = default)
    {
        await StartBufferedCommandAsync(commandText, insertNewline, completionTracker, cancellationToken);
        return await WaitForBufferedCommandAsync(pollTimeSpan, forceCompletionAfterTimeout, cancellationToken);
    }

    /// <summary>
    /// Start a given command with a <see cref="MemoryOutputBuffer"/> set up for a consequent <see cref="WaitForBufferedCommandAsync"/>
    /// call to retrieve its contents.
    /// </summary>
    /// <param name="commandText">The command text to be inserted</param>
    /// <param name="insertNewline">Whether a newline should be put after the command text</param>
    /// <param name="completionTracker">The custom <see cref="ICompletionTracker"/> to use for monitoring this command
    /// (untracked writes are unsupported), <see cref="ExitSignalCompletionTracker"/> is used by default</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    public async Task StartBufferedCommandAsync(
        string commandText,
        bool insertNewline = true,
        ICompletionTracker? completionTracker = null,
        CancellationToken cancellationToken = default)
    {
        if (OutputBuffer is not MemoryOutputBuffer)
        {
            OutputBuffer = new MemoryOutputBuffer();
        }
        
        completionTracker ??= new ExitSignalCompletionTracker();
        await BeginPrimaryWriteAsync(commandText, insertNewline, completionTracker, cancellationToken);
    }

    /// <summary>
    /// Retrieves either the partial or complete state of the <see cref="MemoryOutputBuffer"/> currently configured,
    /// or null if no <see cref="MemoryOutputBuffer"/> is configured. Intended to be used in combination with
    /// <see cref="StartBufferedCommandAsync"/>.
    /// </summary>
    /// <returns>The state of the buffer or null if the buffer is not a <see cref="MemoryOutputBuffer"/></returns>
    public string? TryGetMemoryBufferState()
    {
        if (OutputBuffer is not MemoryOutputBuffer memoryBuffer) return null;
        return _primaryCompletionTracker is null ? memoryBuffer.LastCommit : memoryBuffer.FutureCommitState;
    }

    /// <summary>
    /// Wait for queued buffered command to complete and return its buffered output if a <see cref="MemoryOutputBuffer"/>
    /// is configured. Intended to be used in combination with <see cref="StartBufferedCommandAsync"/>.
    /// </summary>
    /// <param name="pollTimeSpan">The frequency at which the semaphore should be polled</param>
    /// <param name="forceCompletionAfterTimeout">Whether to force completion after the wait has timed out</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor</param>
    /// <returns>The buffer content or null if the buffer is not a <see cref="MemoryOutputBuffer"/></returns>
    public async Task<string?> WaitForBufferedCommandAsync(
        TimeSpan? pollTimeSpan = null,
        bool forceCompletionAfterTimeout = false,
        CancellationToken cancellationToken = default)
    {
        if (OutputBuffer is not MemoryOutputBuffer memoryBuffer) return null;
        
        await WaitForPrimaryAvailabilityAsync(pollTimeSpan, forceCompletionAfterTimeout, cancellationToken);
        return memoryBuffer.LastCommit;
    }

    private async Task WriteInternalAsync(
        string inputText, bool insertNewline, Action finallyBlock, CancellationToken cancellationToken)
    {
        try
        {
            if (insertNewline)
            {
                await _vm.Process!.WriteLineAsync(inputText, cancellationToken);
            }
            else
            {
                await _vm.Process!.WriteAsync(inputText, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw new TtyException($"A TTY write operation has timed out for microVM {_vm.VmId}");
        }
        finally
        {
            finallyBlock();
        }
    }

    private static async Task<bool> PollForSemaphoreAsync(
        SemaphoreSlim semaphore, TimeSpan? pollTimeSpan, Action? completer, CancellationToken cancellationToken)
    {
        pollTimeSpan ??= TimeSpan.FromMilliseconds(1);

        try
        {
            while (true)
            {
                await Task.Delay(pollTimeSpan.Value, cancellationToken);
                if (semaphore.CurrentCount > 0) break;
            }
        }
        catch (OperationCanceledException)
        {
            completer?.Invoke();
            return false;
        }

        return true;
    }
}