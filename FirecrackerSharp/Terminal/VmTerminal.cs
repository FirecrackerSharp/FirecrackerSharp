using System.Text;
using FirecrackerSharp.Boot;
using FirecrackerSharp.Host;
using Serilog;

namespace FirecrackerSharp.Terminal;

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
public class VmTerminal
{
    private static readonly ILogger Logger = Log.ForContext<VmTerminal>();
    
    private readonly Vm _vm;
    
    /// <summary>
    /// Whether this terminal is locked, e.g. another operation is already taking place. As this is merely a TTY,
    /// only one operation can be executed at a time.
    ///
    /// Always check if locked before performing operations, otherwise <see cref="TerminalLockedException"/> will be
    /// thrown!
    /// </summary>
    public bool Locked { get; private set; }

    internal VmTerminal(Vm vm)
    {
        _vm = vm;
    }

    public async Task<TerminalCommand> StartCommandAsync(
        string command,
        string arguments = "",
        uint readTimeoutSeconds = 5,
        uint writeTimeoutSeconds = 3)
    {
        var readSource = new CancellationTokenSource();
        readSource.CancelAfter(TimeSpan.FromSeconds(readTimeoutSeconds));
        await ReadNewAsync(readSource, awaitChanges: false);
        
        var writeSource = new CancellationTokenSource();
        writeSource.CancelAfter(TimeSpan.FromSeconds(writeTimeoutSeconds));
        await WriteNewAsync(writeSource, command + " " + arguments);

        return new TerminalCommand(terminal: this, command, arguments);
    }
    
    internal async Task<string> ReadNewAsync(CancellationTokenSource source, bool awaitChanges = false)
    {
        if (Locked)
        {
            throw new TerminalLockedException("Attempted to perform a terminal read operation when it was locked");
        }
        
        var reader = _vm.Process!.OutputReader;
        
        Locked = true;
        
        var stringBuilder = new StringBuilder();
        var previousNullAmount = 0;

        while (true)
        {
            var innerBuilder = new StringBuilder();
            var anythingFound = false;
            try
            {
                while (await reader.ReadLineAsync(source.Token) is { } line)
                {
                    innerBuilder.AppendLine(line);
                    Console.WriteLine(line);
                    anythingFound = true;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }

            stringBuilder.Append(innerBuilder);

            if (!awaitChanges) break;

            if (!anythingFound)
            {
                if (previousNullAmount > 5) break;

                previousNullAmount++;
                await Task.Delay(10);
            }
            else
            {
                previousNullAmount = 0;
            }
        }

        Locked = false;

        var content = stringBuilder.ToString();

        return content;
    }

    internal async Task WriteNewAsync(CancellationTokenSource source, string content)
    {
        if (Locked)
        {
            throw new TerminalLockedException("Attempted to perform a terminal write operation when it was locked");
        }
        
        Locked = true;

        try
        {
            await _vm.Process!.InputWriter.WriteLineAsync(new StringBuilder(content), source.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TerminalTimeoutException("A terminal write operation timed out");
        }
        finally
        {
            Locked = false;
        }
    }
}