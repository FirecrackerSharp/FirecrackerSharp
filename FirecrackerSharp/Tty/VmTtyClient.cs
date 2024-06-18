using System.Text;
using FirecrackerSharp.Core;
using FirecrackerSharp.Lifecycle;

namespace FirecrackerSharp.Tty;

public class VmTtyClient
{
    private readonly Vm _vm;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsAvailable => _semaphore.CurrentCount > 0;
    
    private readonly StringBuilder _captureBuffer = new();
    private ILogTarget? _captureBufferLogTarget;
    private bool _captureBufferOpen;
    
    private string? _currentExitSignal;
    private bool _isSkipScheduled;

    public event EventHandler<bool>? CaptureBufferOpened;
    public event EventHandler<string>? CaptureBufferClosed;
    public event EventHandler<string>? CaptureBufferReceivedData;

    public Func<string> ExitSignalFactory { get; set; } = () => "exit_" + Random.Shared.Next(1, 100000);
    
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

            if (_currentExitSignal is not null
                && line.Trim() == _currentExitSignal
                && !IsAvailable)
            {
                _semaphore.Release();
            }
            else
            {
                if (_isSkipScheduled)
                {
                    _isSkipScheduled = false;
                    return;
                }
                
                _captureBuffer.Append(line);
                _captureBufferLogTarget?.Receive(line);
                CaptureBufferReceivedData?.Invoke(sender: this, line);
            }
        };
    }

    public async Task<string> StartAndAwaitCommandAsync(
        string commandText,
        bool insertNewline = true,
        ILogTarget? captureBufferLogTarget = null,
        CancellationToken cancellationToken = default)
    {
        await StartCommandAsync(commandText, insertNewline, captureBufferLogTarget, cancellationToken);
        return await AwaitCurrentCommandAsync(cancellationToken);
    }

    public async Task StartCommandAsync(
        string commandText,
        bool insertNewline = true,
        ILogTarget? captureBufferLogTarget = null,
        CancellationToken cancellationToken = default)
    {
        _currentExitSignal = ExitSignalFactory();
        commandText = commandText.Trim();
        
        await WriteAsync(
            $"{commandText} ; echo {_currentExitSignal}",
            insertNewline,
            immediatelyRelease: false,
            cancellationToken: cancellationToken);
        
        await OpenCaptureBufferAsync(
            scheduleSkip: true,
            captureBufferLogTarget,
            bypassSemaphore: true,
            cancellationToken);
    }

    public async Task<string> AwaitCurrentCommandAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        var data = CloseCaptureBuffer();
        return data;
    }

    public async Task<string> InterruptCommandAsync(
        string interruptSignal = "^C",
        bool insertNewline = true,
        CancellationToken cancellationToken = default)
    {
        var output = CloseCaptureBuffer();
        await WriteAsync(interruptSignal, insertNewline, immediatelyRelease: true, cancellationToken);
        return output;
    }

    public async Task WriteAsync(
        string content,
        bool insertNewline = true,
        bool immediatelyRelease = true,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            if (insertNewline)
            {
                await _vm.Process!.StdinWriter.WriteLineAsync(new StringBuilder(content), cancellationToken);
            }
            else
            {
                await _vm.Process!.StdinWriter.WriteAsync(new StringBuilder(content), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw new TtyException($"A TTY write operation has timed out for microVM {_vm.VmId}");
        }
        finally
        {
            if (immediatelyRelease)
            {
                _semaphore.Release();
            }
        }
    }

    public async Task OpenCaptureBufferAsync(
        bool scheduleSkip = false,
        ILogTarget? captureBufferLogTarget = null,
        bool bypassSemaphore = false,
        CancellationToken cancellationToken = default)
    {
        if (_captureBufferOpen) throw new TtyException("Cannot open multiple simultaneous capture buffers");

        if (!bypassSemaphore)
        {
            await _semaphore.WaitAsync(cancellationToken);
        }

        _captureBufferOpen = true;
        
        _captureBufferLogTarget = captureBufferLogTarget;
        _isSkipScheduled = scheduleSkip;
        
        _captureBuffer.Clear();
        CaptureBufferOpened?.Invoke(sender: this, scheduleSkip);
    }

    public string CloseCaptureBuffer()
    {
        if (!_captureBufferOpen) throw new TtyException("Cannot close capture buffer when it isn't open");
        _captureBufferOpen = false;
        
        _captureBufferLogTarget = null;
        _semaphore.Release();
        
        var bufferContent = _captureBuffer.ToString();
        _captureBuffer.Clear();
        CaptureBufferClosed?.Invoke(sender: this, bufferContent);
        return bufferContent;
    }

    public string GetCurrentCaptureBuffer() => _captureBuffer.ToString();
}