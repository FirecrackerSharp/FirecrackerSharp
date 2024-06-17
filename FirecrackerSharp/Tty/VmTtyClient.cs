using System.Text;
using FirecrackerSharp.Core;

namespace FirecrackerSharp.Tty;

public class VmTtyClient
{
    private readonly Vm _vm;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsAvailable => _semaphore.CurrentCount > 0;
    
    internal VmTtyClient(Vm vm)
    {
        _vm = vm;
    }

    internal void RegisterListener()
    {
        
    }

    public Task WaitForAvailabilityAsync(CancellationToken cancellationToken = default)
        => _semaphore.WaitAsync(cancellationToken);
    
    public async Task WriteAsync(
        string content,
        bool insertNewline = true,
        CancellationToken cancellationToken = default)
    {
        await WaitForAvailabilityAsync(cancellationToken);

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
            _semaphore.Release();
        }
    }
}