using System.Text;
using FirecrackerSharp.Boot;
using Serilog;

namespace FirecrackerSharp.Tty;

/// <summary>
/// An interface to a microVM's primary TTY that is exposed by Firecracker every time it is configured through a
/// kernel boot argument (as is highly recommended, so that a graceful shutdown can occur).
/// 
/// This is an <b>extremely</b> limited way to access the microVM: only CLI operations are supported, only 1 operation
/// at a time can be made (accomplished through <see cref="Locked"/> internally). For any case that doesn't fit under
/// these harsh requirements, use CNI networking instead to be able to SSH into the microVM. However, in the case that
/// this method of access suffices, it's preferred to use it in order to avoid having to further open up your microVM
/// to the host.
/// </summary>
public class VmTty
{
    private static readonly ILogger Logger = Log.ForContext<VmTty>();
    
    private readonly Vm _vm;
    private bool _mustRead;
    
    /// <summary>
    /// Whether this terminal is locked, e.g. another operation is already taking place. As this is merely a TTY,
    /// only one operation can be executed at a time.
    ///
    /// Always check if locked before performing operations, otherwise <see cref="TtyLockedException"/> will be
    /// thrown!
    /// </summary>
    public bool Locked { get; private set; }

    internal VmTty(Vm vm)
    {
        _vm = vm;
        _mustRead = true;
    }

    public async Task<TtyCommand> StartCommandAsync(
        string command,
        string arguments = "",
        uint readTimeoutSeconds = 5,
        uint writeTimeoutSeconds = 3)
    {
        if (_mustRead)
        {
            var readSource = new CancellationTokenSource(TimeSpan.FromSeconds(readTimeoutSeconds));
            await ReadNewAsync(readSource, skipFirstLine: false);
            
            _mustRead = false;
        }

        var writeSource = new CancellationTokenSource();
        writeSource.CancelAfter(TimeSpan.FromSeconds(writeTimeoutSeconds));
        await WriteNewAsync(writeSource, command + " " + arguments);

        return new TtyCommand(tty: this, command, arguments, TimeSpan.FromSeconds(readTimeoutSeconds));
    }
    
    internal async Task<string?> ReadNewAsync(CancellationTokenSource source, bool skipFirstLine)
    {
        if (Locked)
        {
            throw new TtyLockedException("Attempted to perform a terminal read operation when it was locked");
        }
        
        var reader = _vm.Process!.OutputReader;
        
        Locked = true;
        var anythingFound = false;
        
        var stringBuilder = new StringBuilder();
        var firstLine = true;

        try
        {
            var readSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
            while (await reader.ReadLineAsync(readSource.Token) is { } line && !source.IsCancellationRequested)
            {
                if (firstLine && skipFirstLine)
                {
                    firstLine = false;
                    continue;
                }
                
                stringBuilder.AppendLine(line);
                anythingFound = true;
            }
        }
        catch (OperationCanceledException)
        {
            // silently ignore since ReadLineAsync unfortunately has a tendency to hang like this
        }

        Locked = false;

        return anythingFound ? stringBuilder.ToString() : null;
    }

    internal async Task WriteNewAsync(CancellationTokenSource source, string content)
    {
        if (Locked)
        {
            throw new TtyLockedException("Attempted to perform a terminal write operation when it was locked");
        }
        
        Locked = true;

        try
        {
            await _vm.Process!.InputWriter.WriteLineAsync(new StringBuilder(content), source.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TtyTimeoutException("A terminal write operation timed out");
        }
        finally
        {
            Locked = false;
        }
    }
}