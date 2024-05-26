using System.Text;
using FirecrackerSharp.Boot;

namespace FirecrackerSharp.Shells;

public class VmShellManager
{
    private readonly Vm _vm;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    internal VmShellManager(Vm vm)
    {
        _vm = vm;
    }

    public async Task<VmShell> StartShellAsync(CancellationToken cancellationToken = new())
    {
        var shell = new VmShell(this);
        var stringId = shell.Id.ToString();

        await WriteToTtyAsync($"screen -dmS {stringId}", cancellationToken);
        
        return shell;
    }

    public async Task<string?> ReadFromTtyAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        var anythingRead = false;
        var stringBuilder = new StringBuilder();

        try
        {
            var readTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
            while (await _vm.Process!.StdoutReader.ReadLineAsync(readTokenSource.Token) is { } line
                   && !cancellationToken.IsCancellationRequested)
            {
                anythingRead = true;

                stringBuilder.AppendLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            // silently ignore since ReadLineAsync just hangs like this
        }
        finally
        {
            _semaphore.Release();
        }

        return anythingRead ? stringBuilder.ToString() : null;
    }

    public async Task WriteToTtyAsync(
        string content,
        CancellationToken cancellationToken,
        bool newline = true,
        bool subsequentlyRead = true)
    {
        await _semaphore.WaitAsync(cancellationToken);
        Console.WriteLine(content);

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
            _semaphore.Release();
        }

        if (subsequentlyRead)
        {
            await ReadFromTtyAsync(cancellationToken);
        }
    }
}