using System.Text;
using FirecrackerSharp.Boot;
using FirecrackerSharp.Host;
using Serilog;

namespace FirecrackerSharp.Terminal;

/// <summary>
/// An interface to a microVM's primary TTY that is exposed by Firecracker every time it is configured through a
/// kernel boot argument (as is highly recommended, so that a graceful shutdown can occur).
/// 
/// This is an <b>extremely</b> limited way to access the microVM: only CLI operations are supported and only 1 operation
/// at a time can be made (accomplished through <see cref="Locked"/> internally). For any case that doesn't fit under
/// these harsh requirements, use CNI networking instead to be able to SSH into the microVM. However, in the case that
/// this method of access suffices, it's preferred to use it in order to avoid having to further open up your microVM
/// to the host.
/// </summary>
public class VmTerminal
{
    private static readonly ILogger Logger = Log.ForContext<VmTerminal>();
    
    private readonly Vm _vm;

    private StreamReader Reader => new(_vm.Process!.StandardOutput);
    private StreamWriter Writer => new(_vm.Process!.StandardInput);
    
    /// <summary>
    /// Whether this terminal is locked, e.g. another operation is already taking place. As this is merely a TTY,
    /// only one operation can be executed at a time.
    ///
    /// Always check if locked before performing operations, otherwise <see cref="TerminalLockedException"/> will be
    /// thrown!
    ///
    /// NOTE: the lock can be "reclaimed" and your operation will be cancelled, if a microVM boot/shutdown is requested
    /// at the same time, as those operations always take precedence.
    /// </summary>
    public bool Locked { get; private set; }
    
    private CancellationTokenSource? _currentSource;

    internal VmTerminal(Vm vm)
    {
        _vm = vm;
    }

    internal async Task LogLifecycleAsync(CancellationTokenSource source)
    {
        if (Locked && _currentSource != null)
        {
            await _currentSource.CancelAsync();
            Logger.Warning("A pending terminal operation for microVM {id} had to be cancelled because of" +
                           " higher urgency of boot/shutdown. The lock was reclaimed", _vm.VmId);
            Locked = false;
        }
        
        await ReadNewAsync(source, lifecycle: true);
    }
    
    private async Task<string> ReadNewAsync(CancellationTokenSource source, bool lifecycle)
    {
        if (Locked)
        {
            throw new TerminalLockedException("Attempted to perform a terminal operation when it was locked");
        }
        
        _currentSource = source;
        using var reader = Reader;
        
        Locked = true;
        
        var stringBuilder = new StringBuilder();
        string? previousLine = null;

        while (!_currentSource.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(_currentSource.Token);

            if (previousLine is "" && line == "") break;

            previousLine = line;
            stringBuilder.AppendLine(line);
        }

        Locked = false;

        var content = stringBuilder.ToString();

        if (_vm.VmConfiguration.Logging.Enabled)
        {
            if (!lifecycle || !_vm.VmConfiguration.Logging.OnlyLogLifecycle)
            {
                await IHostFilesystem.Current.AppendToTextFileAsync(_vm.VmConfiguration.Logging.LogPath, content);
            }
        }

        return content;
    }
}