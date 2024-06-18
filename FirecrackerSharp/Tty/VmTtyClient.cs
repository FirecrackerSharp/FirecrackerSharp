using System.Text;
using FirecrackerSharp.Core;
using FirecrackerSharp.Lifecycle;
using FirecrackerSharp.Tty.CompletionTracking;
using FirecrackerSharp.Tty.OutputBuffering;

namespace FirecrackerSharp.Tty;

public class VmTtyClient
{
    private readonly Vm _vm;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private ICompletionTracker? _currentCompletionTracker;
    private IOutputBuffer? _currentOutputBuffer;
    
    public bool IsAvailable => _semaphore.CurrentCount > 0;
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
            var shouldComplete = false;
            
            if (_currentCompletionTracker is not null)
            {
                shouldComplete = _currentCompletionTracker.CheckReactively(line);
                shouldCapture = _currentCompletionTracker.ShouldCapture(line);
            }

            if (shouldCapture && _currentOutputBuffer is not null)
            {
                _currentOutputBuffer.Receive(line);
            }

            if (shouldComplete) RegisterCompletion();
        };
    }

    public async Task WaitForAvailabilityAsync(
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        pollTimeSpan ??= TimeSpan.FromMilliseconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollTimeSpan.Value, cancellationToken);
            if (IsAvailable) break;
        }
    }

    public async Task WriteAsync(
        string inputText,
        bool insertNewline = true,
        ICompletionTracker? completionTracker = null,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        if (completionTracker is not null)
        {
            inputText = completionTracker.TransformInput(inputText);
        }
        
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
            if (completionTracker is null)
            {
                _semaphore.Release();
            }
            else
            {
                completionTracker.Context = new CompletionTrackerContext(
                    TtyClient: this,
                    TrackingStartTime: DateTimeOffset.UtcNow,
                    InputText: inputText);
                
                _currentCompletionTracker = completionTracker;
                var passiveTask = _currentCompletionTracker.CheckPassively();
                
                if (passiveTask is not null)
                {
                    Task.Run(async () =>
                    {
                        var shouldComplete = await passiveTask;
                        if (shouldComplete) RegisterCompletion();
                    });
                }
            }
        }
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
        if (_currentOutputBuffer is not MemoryOutputBuffer)
        {
            _currentOutputBuffer = new MemoryOutputBuffer();
        }
        
        completionTracker ??= new ExitSignalCompletionTracker();
        await WriteAsync(commandText, insertNewline, completionTracker, cancellationToken);
    }

    public string? TryGetCommandBuffer()
    {
        if (_currentOutputBuffer is not MemoryOutputBuffer memoryBuffer) return null;
        return _currentCompletionTracker is null ? memoryBuffer.LastCommit : memoryBuffer.FutureCommitState;
    }

    public async Task<string?> WaitForBufferedCommandAsync(
        TimeSpan? pollTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        if (_currentOutputBuffer is not MemoryOutputBuffer memoryBuffer) return null;
        
        await WaitForAvailabilityAsync(pollTimeSpan, cancellationToken);
        return memoryBuffer.LastCommit;
    }
    
    private void RegisterCompletion()
    {
        _semaphore.Release();
        _currentCompletionTracker = null;
        _currentOutputBuffer?.Commit();
    }
}