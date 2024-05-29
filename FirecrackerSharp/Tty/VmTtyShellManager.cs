using System.Text;
using FirecrackerSharp.Boot;

namespace FirecrackerSharp.Tty;

/// <summary>
/// Allows CLI communication with a microVM <b>without any networking setup</b> (most commonly, for SSH).
///
/// Before using this module's capabilities, please read through the limitations of this system. If your use case is
/// not affected by them, we recommend this system over SSH/networking. The limitations include: lower performance than
/// SSH, blocking operations (enforced through a <see cref="SemaphoreSlim"/>) except for the fact that multiple commands
/// and shells can execute in parallel, plus a bit less stability overall due to it being a custom-built TTY abstraction.
///
/// In order to achieve the ability for multiple shells and commands to coexist on a single TTY (multiple TTYs are
/// impossible due to technical limitations of Firecracker), the GNU screen multiplexer is used. <b>Thus, screen needs
/// to be installed inside the microVM (preferably simply during the setup of the rootfs) for this system to work.</b>
/// </summary>
public class VmTtyShellManager
{
    private readonly Vm _vm;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    internal VmTtyShellManager(Vm vm)
    {
        _vm = vm;
    }

    /// <summary>
    /// Start up a new <see cref="TtyShell"/> and return it.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for this operation</param>
    /// <returns>The started <see cref="TtyShell"/></returns>
    public async Task<TtyShell> StartShellAsync(CancellationToken cancellationToken = new())
    {
        var shell = new TtyShell(this);
        var stringId = shell.Id.ToString();

        await WriteToTtyAsync($"screen -dmS {stringId}", cancellationToken);
        
        return shell;
    }

    internal async Task<string?> ReadFromTtyAsync(CancellationToken cancellationToken)
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

    internal async Task WriteToTtyAsync(
        string content,
        CancellationToken cancellationToken,
        bool newline = true,
        bool subsequentlyRead = true)
    {
        await _semaphore.WaitAsync(cancellationToken);

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