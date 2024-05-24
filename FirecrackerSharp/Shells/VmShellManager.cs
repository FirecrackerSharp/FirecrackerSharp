using System.Text;
using FirecrackerSharp.Boot;

namespace FirecrackerSharp.Shells;

public class VmShellManager
{
    private readonly Vm _vm;

    private bool _locked;
    public LockStrategy LockStrategy { get; set; } = LockStrategy.Default;

    internal VmShellManager(Vm vm)
    {
        _vm = vm;
    }

    public async Task<VmShell> StartShellAsync(
        CancellationToken readCancellationToken = new(),
        CancellationToken writeCancellationToken = new())
    {
        var shell = new VmShell(this);
        var stringId = shell.Id.ToString();

        await WriteToTtyAsync($"screen -dmS {stringId}", writeCancellationToken);
        
        return shell;
    }

    public async Task<string?> ReadFromTtyAsync(bool skipFirstLine, CancellationToken cancellationToken)
    {
        await EnsureUnlockedAsync();

        _locked = true;
        var anythingRead = false;
        var firstLine = true;

        var stringBuilder = new StringBuilder();

        try
        {
            var readTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
            while (await _vm.Process!.StdoutReader.ReadLineAsync(readTokenSource.Token) is { } line
                   && !cancellationToken.IsCancellationRequested)
            {
                if (firstLine && skipFirstLine)
                {
                    firstLine = false;
                    continue;
                }

                stringBuilder.AppendLine(line);
                anythingRead = true;
            }
        }
        catch (OperationCanceledException)
        {
            // silently ignore since ReadLineAsync just hangs like this
        }
        finally
        {
            _locked = false;
        }

        return anythingRead ? stringBuilder.ToString() : null;
    }

    public async Task WriteToTtyAsync(string content, CancellationToken cancellationToken, bool newline = true)
    {
        await EnsureUnlockedAsync();

        _locked = true;

        try
        {
            if (newline)
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
            throw new TtyException($"A TTY write operation timed out for microVM {_vm.VmId}");
        }
        finally
        {
            _locked = false;
        }
    }

    private async Task EnsureUnlockedAsync()
    {
        if (!_locked) return;

        if (LockStrategy.Type == LockStrategyType.ImmediatelyThrow)
        {
            throw new LockException("The VmShellManager was locked while the LockStrategyType was ImmediatelyThrow");
        }

        var cancellationTokenSource = new CancellationTokenSource();
        
        if (LockStrategy.Type == LockStrategyType.WaitWithTimeout)
        {
            if (!LockStrategy.Timeout.HasValue)
            {
                throw new LockException("The LockStrategyType is WaitWithTimeout but the timeout isn't set");
            }
            
            cancellationTokenSource.CancelAfter(LockStrategy.Timeout!.Value);
        }

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (!_locked) return;
            
            await Task.Delay(LockStrategy.PollFrequency, cancellationTokenSource.Token);
        }
    }
}