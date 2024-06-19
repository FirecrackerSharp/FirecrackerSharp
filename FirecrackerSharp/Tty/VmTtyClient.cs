using System.Text;
using FirecrackerSharp.Core;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tty.CompletionTracking;
using FirecrackerSharp.Tty.OutputBuffering;

namespace FirecrackerSharp.Tty;

public sealed class VmTtyClient
{
    private readonly Vm _vm;
    private readonly SemaphoreSlim _primaryWriteSemaphore = new(1, 1);
    private readonly SemaphoreSlim _intermittentWriteSemaphore = new(1, 1);
    
    private ICompletionTracker? _primaryCompletionTracker;
    private ICompletionTracker? _intermittentCompletionTracker;
    private IOutputBuffer? _currentOutputBuffer;
    
    public bool IsAvailableForPrimaryWrite => _primaryWriteSemaphore.CurrentCount > 0;
    public bool IsAvailableForIntermittentWrite => _intermittentWriteSemaphore.CurrentCount > 0;
    
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
                case VmLifecyclePhase.Boot:
                    _vm.Lifecycle.BootLogTarget.Receive(line);
                    break;
                case VmLifecyclePhase.Active:
                    _vm.Lifecycle.ActiveLogTarget.Receive(line);
                    break;
                case VmLifecyclePhase.Shutdown:
                    _vm.Lifecycle.ShutdownLogTarget.Receive(line);
                    break;
                case VmLifecyclePhase.PreBoot:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_vm.Lifecycle.IsNotActive) return;
            
            var shouldCapture = true;
            var shouldCompletePrimary = false;
            var shouldCompleteIntermittent = false;
            
            if (_primaryCompletionTracker is not null)
            {
                shouldCompletePrimary = _primaryCompletionTracker.CheckReactively(line);
                shouldCapture = _primaryCompletionTracker.ShouldCapture(line);
            }

            if (_intermittentCompletionTracker is not null)
            {
                shouldCompleteIntermittent = _intermittentCompletionTracker.CheckReactively(line);
            }
            
            if (shouldCapture && OutputBuffer is not null)
            {
                OutputBuffer.Receive(line);
            }

            if (shouldCompletePrimary && !IsAvailableForPrimaryWrite) RegisterPrimaryCompletion();
            if (shouldCompleteIntermittent && !IsAvailableForIntermittentWrite) RegisterIntermittentCompletion();
        };
    }

    public Task WaitForPrimaryAvailabilityAsync(
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
        => PollForSemaphoreAsync(_primaryWriteSemaphore, pollTimeSpan, cancellationToken);

    public Task WaitForIntermittentAvailabilityAsync(
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
        => PollForSemaphoreAsync(_intermittentWriteSemaphore, pollTimeSpan, cancellationToken);

    public async Task WritePrimaryAsync(
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
                if (completionTracker is null)
                {
                    _primaryWriteSemaphore.Release();
                    return;
                }

                completionTracker.Context = new CompletionTrackerContext(
                    TtyClient: this,
                    TrackingStartTime: DateTimeOffset.UtcNow,
                    InputText: inputText);

                _primaryCompletionTracker = completionTracker;
                var passiveTask = _primaryCompletionTracker.CheckPassively();

                if (passiveTask is not null)
                {
                    Task.Run(async () =>
                    {
                        var shouldComplete = await passiveTask;
                        if (shouldComplete) RegisterPrimaryCompletion();
                    });
                }
            },
            cancellationToken);
    }

    public async Task WriteIntermittentAsync(
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
                if (completionTracker is null)
                {
                    _intermittentWriteSemaphore.Release();
                    return;
                }

                completionTracker.Context = new CompletionTrackerContext(
                    TtyClient: this,
                    TrackingStartTime: DateTimeOffset.UtcNow,
                    InputText: inputText);

                _intermittentCompletionTracker = completionTracker;
                var passiveTask = _intermittentCompletionTracker.CheckPassively();

                if (passiveTask is not null)
                {
                    Task.Run(async () =>
                    {
                        var shouldComplete = await passiveTask;
                        if (shouldComplete) RegisterIntermittentCompletion();
                    });
                }
            },
            cancellationToken);
    }

    public async Task<string?> RunBufferedCommandAsync(
        string commandText,
        bool insertNewline = true,
        ICompletionTracker? completionTracker = null,
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        await StartBufferedCommandAsync(commandText, insertNewline, completionTracker, cancellationToken);
        return await WaitForBufferedCommandAsync(pollTimeSpan, cancellationToken);
    }

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
        await WritePrimaryAsync(commandText, insertNewline, completionTracker, cancellationToken);
    }

    public string? TryGetCommandBuffer()
    {
        if (OutputBuffer is not MemoryOutputBuffer memoryBuffer) return null;
        return _primaryCompletionTracker is null ? memoryBuffer.LastCommit : memoryBuffer.FutureCommitState;
    }

    public async Task<string?> WaitForBufferedCommandAsync(
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        if (OutputBuffer is not MemoryOutputBuffer memoryBuffer) return null;
        
        await WaitForPrimaryAvailabilityAsync(pollTimeSpan, cancellationToken);
        return memoryBuffer.LastCommit;
    }

    private async Task WriteInternalAsync(
        string inputText, bool insertNewline, Action finallyBlock, CancellationToken cancellationToken)
    {
        try
        {
            if (insertNewline)
            {
                await _vm.Process!.StdinWriter.WriteLineAsync(new StringBuilder(inputText), cancellationToken);
            }
            else
            {
                await _vm.Process!.StdinWriter.WriteAsync(new StringBuilder(inputText), cancellationToken);
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

    private static async Task PollForSemaphoreAsync(SemaphoreSlim semaphore, TimeSpan? pollTimeSpan,
        CancellationToken cancellationToken)
    {
        pollTimeSpan ??= TimeSpan.FromMilliseconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollTimeSpan.Value, cancellationToken);
            if (semaphore.CurrentCount > 0) break;
        }
    }
    
    private void RegisterPrimaryCompletion()
    {
        _primaryWriteSemaphore.Release();
        _primaryCompletionTracker = null;
        OutputBuffer?.Commit();
    }

    private void RegisterIntermittentCompletion()
    {
        _intermittentWriteSemaphore.Release();
        _intermittentCompletionTracker = null;
    }
}